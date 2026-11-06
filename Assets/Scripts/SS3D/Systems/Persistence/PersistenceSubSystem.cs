using Coimbra.Services.Events;
using Cysharp.Threading.Tasks;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Persistence;
using SS3D.Data.Management;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Permissions.Events;
using SS3D.Systems.Area;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Electricity;
using SS3D.Systems.Furniture.Disposal;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.SpawnPoints;
using FishNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Persistence
{
    /// <summary>
    /// Server-only orchestrator for layered contributor-based persistence.
    /// </summary>
    public sealed class PersistenceSubSystem : SubSystem
    {
        private readonly IPersistenceStore _store = new EnvelopePersistenceStore();
        private readonly List<IPersistenceContributor> _contributors = new();
        private bool _serverMetaLoaded;

        // Only successful operations establish an editor save point / template identity.
        public bool IsRestoringStationTemplate { get; private set; }
        private bool _incompleteStationRestore;
        public event Action<string> StationTemplateSaved;
        public event Action<string> StationTemplateLoaded;

        public event Action<PersistenceLayer> OnBeforeRestore;

        public event Action<PersistenceLayer> OnAfterRestore;

        public event Action<PersistenceLayer> OnBeforeCapture;

        protected override void OnAwake()
        {
            base.OnAwake();
            // Contributors must exist before LoadServerMeta. Hub NetworkBehaviour.OnStartServer
            // can run after Awakes but before Unity Start — keep registration here, not in OnStart.
            RegisterBuiltInContributors();
        }

        protected override void OnStart()
        {
            base.OnStart();
            AddHandle(UserPermissionsChangedEvent.AddListener(HandleUserPermissionsChanged));

            // Own server-meta boot here — do not call from Tile or other domains.
            if (InstanceFinder.IsServer)
            {
                LoadServerMeta();
            }
        }

        public bool LoadServerMeta()
        {
            if (_serverMetaLoaded)
            {
                return true;
            }

            // Defensive: contributors must exist before restore (Awake normally registers them).
            if (_contributors.Count == 0)
            {
                RegisterBuiltInContributors();
            }

            _store.TryLoad(PersistencePaths.ServerMetaPermissions, out PersistenceEnvelope envelope);
            RestoreServerMeta(envelope);
            _serverMetaLoaded = true;
            return true;
        }

        public bool SaveServerMeta()
        {
            PersistenceEnvelope envelope = CaptureServerMeta();
            return _store.TrySave(PersistencePaths.ServerMetaPermissions, envelope, overwrite: true);
        }

        public bool AppendRoundHistory(RoundHistoryEntry entry)
        {
            return RoundHistoryStore.Append(entry);
        }

        public void RegisterContributor(IPersistenceContributor contributor)
        {
            if (contributor == null || _contributors.Any(existing => existing.ContributorId == contributor.ContributorId))
            {
                return;
            }

            _contributors.Add(contributor);
        }

        public bool SaveStationTemplate(string templateName, bool overwrite)
        {
            if (IsRestoringStationTemplate || _incompleteStationRestore) return false;
            string path = GetStationTemplatePath(templateName);
            PersistenceEnvelope envelope = CaptureStationTemplate(templateName);
            bool saved = _store.TrySave(path, envelope, overwrite);
            if (saved) StationTemplateSaved?.Invoke(templateName);
            return saved;
        }

        /// <summary>
        /// The station as Save would write it now, as JSON, without writing anything: a bug report bundle
        /// carries the current map with unsaved edits and leaves the saved file alone.
        /// </summary>
        public string CaptureStationTemplateJson(string templateName)
        {
            if (IsRestoringStationTemplate || _incompleteStationRestore) return null;
            return UnityEngine.JsonUtility.ToJson(CaptureStationTemplate(templateName));
        }

        public bool LoadStationTemplate(string templateName)
        {
            // Sync / EditMode path — no frame yields (can freeze Editor on MetaStation-scale maps).
            return LoadStationTemplateInternal(templateName, timeSlice: false);
        }

        public UniTask<bool> LoadStationTemplateAsync(string templateName) =>
            LoadStationTemplateInternalAsync(templateName, timeSlice: true);

        public bool LoadMostRecentStationTemplate()
        {
            string templateName = GetMostRecentTemplateName();
            if (string.IsNullOrEmpty(templateName))
            {
                Log.Warning(this, "No station templates found to load");
                return false;
            }

            return LoadStationTemplate(templateName);
        }

        public async UniTask<bool> LoadMostRecentStationTemplateAsync()
        {
            string templateName = GetMostRecentTemplateName();
            if (string.IsNullOrEmpty(templateName))
            {
                Log.Warning(this, "No station templates found to load");
                return false;
            }

            return await LoadStationTemplateAsync(templateName);
        }

        private bool LoadStationTemplateInternal(string templateName, bool timeSlice)
        {
            string path = GetStationTemplatePath(templateName);
            if (!TryLoadEnvelope(path, templateName, out PersistenceEnvelope envelope))
            {
                return false;
            }

            if (IsRestoringStationTemplate) return false;
            IsRestoringStationTemplate = true;
            _incompleteStationRestore = true;
            try
            {
                RestoreStationTemplate(envelope, templateName, timeSlice);
                _incompleteStationRestore = false;
                StationTemplateLoaded?.Invoke(templateName);
                return true;
            }
            finally { IsRestoringStationTemplate = false; }
        }

        private async UniTask<bool> LoadStationTemplateInternalAsync(string templateName, bool timeSlice)
        {
            string path = GetStationTemplatePath(templateName);
            if (!TryLoadEnvelope(path, templateName, out PersistenceEnvelope envelope))
            {
                return false;
            }

            if (IsRestoringStationTemplate) return false;
            IsRestoringStationTemplate = true;
            _incompleteStationRestore = true;
            try
            {
                await RestoreStationTemplateAsync(envelope, templateName, timeSlice);
                _incompleteStationRestore = false;
                StationTemplateLoaded?.Invoke(templateName);
                return true;
            }
            finally { IsRestoringStationTemplate = false; }
        }

        public bool StationTemplateExists(string templateName)
        {
            return _store.List(PersistencePaths.StationTemplates).Contains(templateName);
        }

        public IReadOnlyList<string> ListStationTemplates()
        {
            // Testers import shared maps by dropping a zip into StationTemplates/.
            MapFileSharing.ImportDroppedArchives();

            var names = new HashSet<string>(_store.List(PersistencePaths.StationTemplates));
            foreach (string legacyName in _store.List(PersistencePaths.LegacyTilemaps))
            {
                names.Add(legacyName);
            }

            return names.OrderBy(name => name).ToList();
        }

        private void RegisterBuiltInContributors()
        {
            RegisterContributor(new TileMapPersistenceContributor(() => SubSystems.Get<TileSubSystem>()));
            RegisterContributor(new AreaPersistenceContributor(
                () => SubSystems.Get<AreaSubSystem>(),
                () => SubSystems.Get<TileSubSystem>()));
            RegisterContributor(new SpawnPointPersistenceContributor(() => SubSystems.Get<TileSubSystem>()));
            RegisterContributor(new GasMarkerPersistenceContributor(() => SubSystems.Get<TileSubSystem>()));
            RegisterContributor(new PermissionsPersistenceContributor(() => SubSystems.Get<PermissionSubSystem>()));
        }

        private void HandleUserPermissionsChanged(ref EventContext context, in UserPermissionsChangedEvent e)
        {
            if (!InstanceFinder.IsServer)
            {
                return;
            }

            SaveServerMeta();
        }

        private PersistenceEnvelope CaptureServerMeta()
        {
            OnBeforeCapture?.Invoke(PersistenceLayer.ServerMeta);

            var envelope = new PersistenceEnvelope
            {
                schemaVersion = PersistenceEnvelope.CurrentSchemaVersion,
                envelopeType = PersistenceEnvelope.ServerMetaType,
                createdAt = DateTime.UtcNow.ToString("o"),
                gameVersion = UnityEngine.Application.version,
            };

            foreach (IPersistenceContributor contributor in GetContributors(PersistenceLayer.ServerMeta))
            {
                object payload = contributor.Capture();
                if (payload == null)
                {
                    continue;
                }

                envelope.chunks.Add(new PersistenceChunk
                {
                    contributorId = contributor.ContributorId,
                    payloadJson = JsonUtility.ToJson(payload),
                });
            }

            return envelope;
        }

        private void RestoreServerMeta(PersistenceEnvelope envelope)
        {
            OnBeforeRestore?.Invoke(PersistenceLayer.ServerMeta);

            var context = new PersistenceContext
            {
                IsTemplateRestore = false,
                TemplateName = string.Empty,
            };

            foreach (IPersistenceContributor contributor in GetContributors(PersistenceLayer.ServerMeta))
            {
                object payload = null;
                if (envelope?.chunks != null)
                {
                    PersistenceChunk chunk = envelope.chunks.FirstOrDefault(
                        candidate => candidate.contributorId == contributor.ContributorId);

                    if (!string.IsNullOrEmpty(chunk?.payloadJson))
                    {
                        payload = DeserializePayload(contributor, chunk.payloadJson);
                    }
                }

                contributor.Restore(payload, context);
            }

            OnAfterRestore?.Invoke(PersistenceLayer.ServerMeta);
        }

        private PersistenceEnvelope CaptureStationTemplate(string templateName)
        {
            OnBeforeCapture?.Invoke(PersistenceLayer.StationTemplate);

            var envelope = new PersistenceEnvelope
            {
                schemaVersion = PersistenceEnvelope.CurrentSchemaVersion,
                envelopeType = PersistenceEnvelope.StationTemplateType,
                createdAt = DateTime.UtcNow.ToString("o"),
                gameVersion = UnityEngine.Application.version,
            };

            foreach (IPersistenceContributor contributor in GetContributors(PersistenceLayer.StationTemplate))
            {
                object payload = contributor.Capture();
                if (payload == null)
                {
                    continue;
                }

                envelope.chunks.Add(new PersistenceChunk
                {
                    contributorId = contributor.ContributorId,
                    payloadJson = JsonUtility.ToJson(payload),
                });
            }

            return envelope;
        }

        private void RestoreStationTemplate(PersistenceEnvelope envelope, string templateName, bool timeSlice)
        {
            IEnumerator routine = RestoreStationTemplateRoutine(envelope, templateName, timeSlice);
            while (routine.MoveNext())
            {
            }
        }

        private async UniTask RestoreStationTemplateAsync(
            PersistenceEnvelope envelope,
            string templateName,
            bool timeSlice)
        {
            IEnumerator routine = RestoreStationTemplateRoutine(envelope, templateName, timeSlice);
            while (routine.MoveNext())
                await UniTask.Yield();
        }

        private IEnumerator RestoreStationTemplateRoutine(
            PersistenceEnvelope envelope,
            string templateName,
            bool timeSlice)
        {
            OnBeforeRestore?.Invoke(PersistenceLayer.StationTemplate);

            // Direct notify — WorldReadiness is DDOL and may not have bound to OnBeforeRestore yet
            // when Persistence lives on the Online hub.
            if (SubSystems.TryGet(out WorldReadiness.WorldReadinessSubSystem readinessBefore))
            {
                readinessBefore.NotifyStationTemplateRestoreBeginning();
            }

            var context = new PersistenceContext
            {
                IsTemplateRestore = true,
                TemplateName = templateName,
            };

            bool hasElectricity = SubSystems.TryGet(out ElectricitySubSystem electricity);
            bool hasAtmos = SubSystems.TryGet(out AtmosSubSystem atmos);
            bool hasDisposal = SubSystems.TryGet(out DisposalSubSystem disposal);
            bool priorAtmosPaused = false;
            TileMap map = SubSystems.TryGet(out TileSubSystem tileSystem) ? tileSystem.CurrentMap : null;

            // APCs spawn mid-tile-placement and would flood against an incomplete map (missing
            // chunks look like empty space). Defer flood until every contributor has finished.
            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                areaSubSystem.BeginDeferredAreaFlood();
            }

            if (hasElectricity)
                electricity.SuspendCircuitUpdates(true);
            if (hasAtmos)
            {
                priorAtmosPaused = atmos.SimulationPaused;
                atmos.SimulationPaused = true;
            }

            if (hasDisposal)
                disposal.BeginDeferredNetworkRebuild();

            map?.BeginBulkMutation();

            try
            {
                foreach (IPersistenceContributor contributor in GetContributors(PersistenceLayer.StationTemplate))
                {
                    PersistenceChunk chunk = envelope.chunks?.FirstOrDefault(
                        candidate => candidate.contributorId == contributor.ContributorId);

                    if (chunk == null || string.IsNullOrEmpty(chunk.payloadJson))
                    {
                        continue;
                    }

                    object payload = DeserializePayload(contributor, chunk.payloadJson);

                    if (contributor.ContributorId == TileMapPersistenceContributor.ContributorIdValue
                        && payload is SavedTileMap savedTileMap
                        && map != null)
                    {
                        IEnumerator load = map.LoadRoutine(
                            savedTileMap,
                            invokeMapLoadedEvent: false,
                            yieldFrames: timeSlice);
                        while (load.MoveNext())
                            yield return load.Current;
                        continue;
                    }

                    contributor.Restore(payload, context);
                }
            }
            finally
            {
                map?.EndBulkMutation();

                if (hasDisposal)
                    disposal.EndDeferredNetworkRebuild();
                if (hasElectricity)
                    electricity.SuspendCircuitUpdates(false);
                if (hasAtmos)
                    atmos.SimulationPaused = priorAtmosPaused;

                if (SubSystems.TryGet(out AreaSubSystem areaAfterRestore))
                {
                    areaAfterRestore.EndDeferredAreaFlood();
                }
            }

            OnAfterRestore?.Invoke(PersistenceLayer.StationTemplate);

            if (SubSystems.TryGet(out WorldReadiness.WorldReadinessSubSystem readinessAfter))
            {
                readinessAfter.NotifyTileMapLoaded();
            }
        }

        private bool TryLoadEnvelope(string path, string templateName, out PersistenceEnvelope envelope)
        {
            if (_store.TryLoad(path, out envelope))
            {
                return true;
            }

            // StationTemplates may be empty while legacy Tilemaps still has the map — don't warn yet.
            if (LocalStorage.TryReadRaw(path, out string rawJson)
                && LegacyTileMapMigrator.TryWrapLegacyJson(rawJson, templateName, out envelope))
            {
                return true;
            }

            string legacyPath = PersistencePaths.LegacyTilemaps + "/" + templateName;
            if (LocalStorage.TryReadRaw(legacyPath, out rawJson)
                && LegacyTileMapMigrator.TryWrapLegacyJson(rawJson, templateName, out envelope))
            {
                return true;
            }

            Log.Warning(this, "No station template found for {templateName} in StationTemplates or Tilemaps", Logs.Generic, templateName);
            envelope = null;
            return false;
        }

        private static object DeserializePayload(IPersistenceContributor contributor, string payloadJson)
        {
            return contributor.ContributorId switch
            {
                TileMapPersistenceContributor.ContributorIdValue => JsonUtility.FromJson<SavedTileMap>(payloadJson),
                AreaPersistenceContributor.ContributorIdValue => JsonUtility.FromJson<SavedAreaChunkPayload>(payloadJson),
                SpawnPointPersistenceContributor.ContributorIdValue => JsonUtility.FromJson<SavedSpawnPointChunkPayload>(payloadJson),
                GasMarkerPersistenceContributor.ContributorIdValue => JsonUtility.FromJson<SavedGasMarkerChunkPayload>(payloadJson),
                PermissionsPersistenceContributor.ContributorIdValue => JsonUtility.FromJson<SavedPermissionsPayload>(payloadJson),
                _ => payloadJson,
            };
        }

        private IEnumerable<IPersistenceContributor> GetContributors(PersistenceLayer layer)
        {
            return _contributors
                .Where(contributor => contributor.Layer == layer)
                .OrderBy(contributor => contributor.LoadOrder);
        }

        private static string GetStationTemplatePath(string templateName)
        {
            return PersistencePaths.StationTemplates + "/" + templateName;
        }

        private string GetMostRecentTemplateName()
        {
            string stationTemplate = LocalStorage.GetMostRecentFileName(PersistencePaths.StationTemplates);
            string legacyTemplate = LocalStorage.GetMostRecentFileName(PersistencePaths.LegacyTilemaps);

            if (string.IsNullOrEmpty(stationTemplate))
            {
                return legacyTemplate;
            }

            if (string.IsNullOrEmpty(legacyTemplate))
            {
                return stationTemplate;
            }

            // Prefer the file with the latest write time across both folders.
            string stationPath = PersistencePaths.StationTemplates + "/" + stationTemplate;
            string legacyPath = PersistencePaths.LegacyTilemaps + "/" + legacyTemplate;
            if (LocalStorage.GetLastWriteTimeUtc(stationPath) >= LocalStorage.GetLastWriteTimeUtc(legacyPath))
            {
                return stationTemplate;
            }

            return legacyTemplate;
        }
    }
}
