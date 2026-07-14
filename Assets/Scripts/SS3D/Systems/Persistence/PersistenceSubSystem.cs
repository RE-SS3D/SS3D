using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Persistence;
using SS3D.Data.Management;
using SS3D.Logging;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using System;
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

        public event Action<PersistenceLayer> OnBeforeRestore;

        public event Action<PersistenceLayer> OnAfterRestore;

        public event Action<PersistenceLayer> OnBeforeCapture;

        protected override void OnStart()
        {
            base.OnStart();
            RegisterBuiltInContributors();
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
            string path = GetStationTemplatePath(templateName);
            PersistenceEnvelope envelope = CaptureStationTemplate(templateName);
            return _store.TrySave(path, envelope, overwrite);
        }

        public bool LoadStationTemplate(string templateName)
        {
            string path = GetStationTemplatePath(templateName);
            if (!TryLoadEnvelope(path, templateName, out PersistenceEnvelope envelope))
            {
                return false;
            }

            RestoreStationTemplate(envelope, templateName);
            return true;
        }

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

        public bool StationTemplateExists(string templateName)
        {
            return _store.List(PersistencePaths.StationTemplates).Contains(templateName);
        }

        public IReadOnlyList<string> ListStationTemplates()
        {
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

        private void RestoreStationTemplate(PersistenceEnvelope envelope, string templateName)
        {
            OnBeforeRestore?.Invoke(PersistenceLayer.StationTemplate);

            var context = new PersistenceContext
            {
                IsTemplateRestore = true,
                TemplateName = templateName,
            };

            foreach (IPersistenceContributor contributor in GetContributors(PersistenceLayer.StationTemplate))
            {
                PersistenceChunk chunk = envelope.chunks?.FirstOrDefault(
                    candidate => candidate.contributorId == contributor.ContributorId);

                if (chunk == null || string.IsNullOrEmpty(chunk.payloadJson))
                {
                    continue;
                }

                object payload = DeserializePayload(contributor, chunk.payloadJson);
                contributor.Restore(payload, context);
            }

            OnAfterRestore?.Invoke(PersistenceLayer.StationTemplate);
        }

        private bool TryLoadEnvelope(string path, string templateName, out PersistenceEnvelope envelope)
        {
            if (_store.TryLoad(path, out envelope))
            {
                return true;
            }

            string rawJson = EnvelopePersistenceStore.ReadRawJson(path);
            if (LegacyTileMapMigrator.TryWrapLegacyJson(rawJson, templateName, out envelope))
            {
                return true;
            }

            string legacyPath = PersistencePaths.LegacyTilemaps + "/" + templateName;
            rawJson = EnvelopePersistenceStore.ReadRawJson(legacyPath);
            return LegacyTileMapMigrator.TryWrapLegacyJson(rawJson, templateName, out envelope);
        }

        private static object DeserializePayload(IPersistenceContributor contributor, string payloadJson)
        {
            return contributor.ContributorId switch
            {
                TileMapPersistenceContributor.ContributorIdValue => JsonUtility.FromJson<SavedTileMap>(payloadJson),
                AreaPersistenceContributor.ContributorIdValue => JsonUtility.FromJson<SavedAreaChunkPayload>(payloadJson),
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
