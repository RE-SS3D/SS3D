using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Substances
{
    /// <summary>
    /// Networked liquid vessel. Mixture entries sync via <see cref="SyncList{T}"/>; seed
    /// <see cref="InitialMixture"/> on the server only.
    /// </summary>
    public class SubstanceContainer : InteractionTargetNetworkBehaviour
    {
        public static event Action<SubstanceContainer, HazardKind, Vector3> HazardOccurred;

        [SerializeField]
        private List<MixtureEntry> _initialMixture = new();

        [SerializeField]
        private float _defaultVolumeMl = 100f;

        [SyncVar]
        private float _capacityMl;

        [SyncVar]
        private float _currentVolumeMl;

        [SyncVar]
        private float _temperatureKelvin = SubstanceConstants.DefaultAmbientKelvin;

        [SyncVar]
        private bool _locked;

        [SyncVar]
        private bool _initialised;

        [SyncObject]
        private readonly SyncList<MixtureEntry> _entries = new();
        private readonly List<MixtureEntry> _working = new();
        private ReadOnlyCollection<MixtureEntry> _workingView;
        private bool _temperatureTickRegistered;

        public float CapacityMl => _capacityMl;
        public float CurrentVolumeMl => _currentVolumeMl;
        public float RemainingVolumeMl => Mathf.Max(0f, _capacityMl - _currentVolumeMl);
        public float TemperatureKelvin => _temperatureKelvin;
        public bool Locked => _locked;
        public bool CanTransfer => !_locked;
        public bool IsEmpty => _currentVolumeMl <= SubstanceConstants.VolumeEpsilonMl;

        /// <summary>
        /// Live read of mixture. Prefer iterating once; do not call hot paths that allocate.
        /// </summary>
        public IReadOnlyList<MixtureEntry> Entries
        {
            get
            {
                SyncWorkingFromNetworked();
                return _workingView ??= _working.AsReadOnly();
            }
        }

        public event Action<SubstanceContainer> ContentsChanged;

        protected override void OnAwake()
        {
            base.OnAwake();
            _entries.OnChange += HandleEntriesChanged;
        }

        public override void OnStopNetwork()
        {
            _entries.OnChange -= HandleEntriesChanged;
            UnregisterTemperatureTick();
            base.OnStopNetwork();
        }

        /// <summary>
        /// Seed prefab <see cref="_initialMixture"/> on the server only.
        /// Writing SyncVars from Unity <c>Start</c> on pure clients hits the smoke denylist.
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();

            if (!_initialised)
            {
                if (_capacityMl <= 0f)
                {
                    _capacityMl = _defaultVolumeMl;
                }

                _temperatureKelvin = SampleAmbientKelvin();
                if (_initialMixture != null)
                {
                    for (int i = 0; i < _initialMixture.Count; i++)
                    {
                        MixtureEntry entry = _initialMixture[i];
                        if (string.IsNullOrEmpty(entry.ReagentId) || entry.VolumeMl <= 0f)
                        {
                            continue;
                        }

                        Add(entry.ReagentId, entry.VolumeMl);
                    }
                }

                _initialised = true;
            }

            RegisterTemperatureTick();
        }

        public override void OnStopServer()
        {
            UnregisterTemperatureTick();
            base.OnStopServer();
        }

        [Server]
        public void Init(float capacityMl, bool locked)
        {
            if (_initialised)
            {
                Log.Warning(this, "already initialised, returning");
                return;
            }

            _capacityMl = capacityMl;
            _locked = locked;
            _temperatureKelvin = SubstanceConstants.DefaultAmbientKelvin;
            _initialised = true;
        }

        [Server]
        public void SetLocked(bool locked) => _locked = locked;

        [Server]
        public void ChangeCapacity(float newCapacityMl) => _capacityMl = Mathf.Max(0f, newCapacityMl);

        [Server]
        public float Add(string reagentId, float volumeMl)
        {
            if (!CanTransfer || volumeMl <= 0f || string.IsNullOrEmpty(reagentId))
            {
                return 0f;
            }

            SyncWorkingFromNetworked();
            float accepted = MixtureOperations.Add(_working, reagentId, volumeMl, RemainingVolumeMl);
            if (accepted > 0f)
            {
                PushWorkingToNetworked();
                RecalculateVolume();
                OnContentsChanged();
                ResolveReactionsAfterPour();
            }

            return accepted;
        }

        [Server]
        public float Remove(string reagentId, float volumeMl)
        {
            if (!CanTransfer)
            {
                return 0f;
            }

            SyncWorkingFromNetworked();
            float removed = MixtureOperations.Remove(_working, reagentId, volumeMl);
            if (removed > 0f)
            {
                PushWorkingToNetworked();
                RecalculateVolume();
                OnContentsChanged();
            }

            return removed;
        }

        [Server]
        public void TransferVolume(SubstanceContainer other, float volumeMl)
        {
            if (other == null || !CanTransfer || !other.CanTransfer || volumeMl <= 0f || IsEmpty)
            {
                return;
            }

            float transfer = Mathf.Min(volumeMl, CurrentVolumeMl, other.RemainingVolumeMl);
            if (transfer <= SubstanceConstants.VolumeEpsilonMl)
            {
                return;
            }

            SyncWorkingFromNetworked();
            List<MixtureEntry> removed = MixtureOperations.RemoveProportional(_working, transfer);
            PushWorkingToNetworked();
            RecalculateVolume();

            for (int i = 0; i < removed.Count; i++)
            {
                other.Add(removed[i].ReagentId, removed[i].VolumeMl);
            }

            OnContentsChanged();
        }

        [Server]
        public void Empty()
        {
            _entries.Clear();
            _working.Clear();
            RecalculateVolume();
            OnContentsChanged();
        }

        [Server]
        public void ResolveReactionsAfterPour()
        {
            if (!SubSystems.TryGet(out SubstancesSubSystem substances) || substances.Resolver == null)
            {
                return;
            }

            SyncWorkingFromNetworked();
            ReactionResult result = substances.Resolver.Resolve(_working, _temperatureKelvin);
            if (result.Outcome == ReactionOutcome.Success)
            {
                PushWorkingToNetworked();
                RecalculateVolume();
                _temperatureKelvin += result.TemperatureDeltaKelvin;
                OnContentsChanged();
                MaybeIgniteFromFlash();
            }
            else if (result.Outcome is ReactionOutcome.NearMiss or ReactionOutcome.Incompatible)
            {
                RaiseHazard(result.Hazard);
            }
        }

        public float GetVolume(string reagentId)
        {
            SyncWorkingFromNetworked();
            return MixtureOperations.GetVolume(_working, reagentId);
        }

        public Color GetBlendColor()
        {
            SyncWorkingFromNetworked();
            if (!SubSystems.TryGet(out SubstancesSubSystem substances))
            {
                return new Color(1f, 1f, 1f, 0.2f);
            }

            return MixtureOperations.BlendColor(_working, substances.Registry);
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new TransferSubstanceInteraction(),
                new PourSubstanceInteraction(),
            };
        }

        [Server]
        private void MaybeIgniteFromFlash()
        {
            if (!SubSystems.TryGet(out SubstancesSubSystem substances))
            {
                return;
            }

            SyncWorkingFromNetworked();
            float flash = MixtureOperations.LowestFlashPointKelvin(_working, substances.Registry);
            if (_temperatureKelvin >= flash && !float.IsInfinity(flash))
            {
                RaiseHazard(HazardKind.Ignition);
            }
        }

        [Server]
        private void RaiseHazard(HazardKind hazard)
        {
            if (hazard == HazardKind.None)
            {
                return;
            }

            HazardOccurred?.Invoke(this, hazard, transform.position);
            substancesLogHazard(hazard);
        }

        private void substancesLogHazard(HazardKind hazard)
        {
            Log.Information(this, "Substance hazard {hazard} at {pos}", Logs.Generic, hazard, transform.position);
        }

        private void HandleEntriesChanged(SyncListOperation op, int index, MixtureEntry oldItem, MixtureEntry newItem, bool asServer)
        {
            if (!asServer)
            {
                SyncWorkingFromNetworked();
                ContentsChanged?.Invoke(this);
            }
        }

        private void SyncWorkingFromNetworked()
        {
            _working.Clear();
            for (int i = 0; i < _entries.Count; i++)
            {
                _working.Add(_entries[i]);
            }
        }

        private void PushWorkingToNetworked()
        {
            _entries.Clear();
            for (int i = 0; i < _working.Count; i++)
            {
                _entries.Add(_working[i]);
            }
        }

        private void RecalculateVolume()
        {
            _currentVolumeMl = MixtureOperations.TotalVolumeMl(_working);
        }

        private void OnContentsChanged()
        {
            ContentsChanged?.Invoke(this);
        }

        private void RegisterTemperatureTick()
        {
            if (_temperatureTickRegistered || !IsServer)
            {
                return;
            }

            if (SubSystems.TryGet(out SubstancesSubSystem substances))
            {
                substances.RegisterContainer(this);
                _temperatureTickRegistered = true;
            }
        }

        private void UnregisterTemperatureTick()
        {
            if (!_temperatureTickRegistered)
            {
                return;
            }

            if (SubSystems.TryGet(out SubstancesSubSystem substances))
            {
                substances.UnregisterContainer(this);
            }

            _temperatureTickRegistered = false;
        }

        [Server]
        internal void TickTemperature(float deltaTime)
        {
            float ambient = SampleAmbientKelvin();
            _temperatureKelvin = Mathf.Lerp(
                _temperatureKelvin,
                ambient,
                1f - Mathf.Exp(-SubstanceConstants.TemperatureDecayPerSecond * deltaTime));
            MaybeIgniteFromFlash();
        }

        private float SampleAmbientKelvin()
        {
            if (!SubSystems.TryGet(out TileSubSystem tile)
                || tile.CurrentMap == null
                || !SubSystems.TryGet(out AtmosSubSystem atmos))
            {
                return SubstanceConstants.DefaultAmbientKelvin;
            }

            TileCoord coord = tile.QueryService.WorldToTile(transform.position, tile.CurrentMap.MapId);
            if (atmos.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info))
            {
                return info.Temperature;
            }

            return SubstanceConstants.DefaultAmbientKelvin;
        }
    }
}
