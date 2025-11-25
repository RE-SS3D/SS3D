using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Content.Systems.Interactions;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Tile;
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Atmospherics
{
    public class FilterAtmosObject : TrinaryAtmosDevice
    {
        public event Action<bool, CoreAtmosGasses> OnUpdateFilterGas;

        public event Action<bool> OnUpdateActive;

        public event Action<float> OnUpdateFlux;

        private const float MaxPressure = 4500f;

        [SyncVar(OnChange = nameof(SyncFlux))]
        private float _litersPerSecond = 1f;

        [SyncVar(OnChange = nameof(SyncFilterActive))]
        private bool _filterActive;

        [SyncVar(OnChange = nameof(SyncFilterOxygen))]
        private bool _filterOxygen;

        [SyncVar(OnChange = nameof(SyncFilterNitrogen))]
        private bool _filterNitrogen;

        [SyncVar(OnChange = nameof(SyncFilterCarbonDioxyde))]
        private bool _filterCarbonDioxyde;

        [SyncVar(OnChange = nameof(SyncFilterPlasma))]
        private bool _filterPlasma;

        private TileLayer _pipeLayer = TileLayer.PipeLeft;

        [FormerlySerializedAs("FilterViewPrefab")]
        [SerializeField]
        private GameObject _filterViewPrefab;

        public bool FilterActive => _filterActive;

        public float LitersPerSecond => _litersPerSecond;

        [ServerRpc(RequireOwnership = false)]
        public void SetFilterActive(bool filterActive) => _filterActive = filterActive;

        [ServerRpc(RequireOwnership = false)]
        public void SetFlux(float litersPerSecond) => _litersPerSecond = litersPerSecond;

        [Server]
        public override void StepAtmos(float dt)
        {
            base.StepAtmos(dt);

            if (!_filterActive || !AllPipesConnected)
            {
                return;
            }

            // Both outputs must not be blocked
            if (SidePipe.AtmosObject.Pressure > MaxPressure && FrontPipe.AtmosObject.Pressure > MaxPressure)
            {
                return;
            }

            AtmosObject atmosInput = BackPipe.AtmosObject;
            float maxMolesToTransfer = (atmosInput.Pressure * _litersPerSecond * dt) / (GasConstants.GasConstant * atmosInput.Temperature);
            float4 molesToTransfer = atmosInput.CoreGassesProportions * math.max(maxMolesToTransfer, atmosInput.TotalMoles);

            if (math.all(molesToTransfer == 0))
            {
                return;
            }

            bool4 filterCoreGasses = new bool4(_filterOxygen, _filterNitrogen, _filterCarbonDioxyde, _filterPlasma);
            Subsystems.Get<PipeSubSystem>().AddCoreGasses(SidePipePosition, molesToTransfer * (int4)filterCoreGasses, _pipeLayer);
            Subsystems.Get<PipeSubSystem>().AddCoreGasses(FrontPipePosition, molesToTransfer * (int4)!filterCoreGasses, _pipeLayer);
            Subsystems.Get<PipeSubSystem>().RemoveCoreGasses(BackPipePosition, molesToTransfer, _pipeLayer);
        }

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new SimpleInteraction
                {
                    Name = _filterActive ? "Stop filter" : "Start filter", Interact = FilterInteract, RangeCheck = true,
                },
            };
        }

        [Client]
        public void FilterGas(bool isFiltering, CoreAtmosGasses gas)
        {
            RpcFilterGas(isFiltering, gas);
        }

        public bool IsFiltering(CoreAtmosGasses gas)
        {
            switch (gas)
            {
                case CoreAtmosGasses.Nitrogen:
                    return _filterNitrogen;
                case CoreAtmosGasses.Oxygen:
                    return _filterOxygen;
                case CoreAtmosGasses.CarbonDioxide:
                    return _filterCarbonDioxyde;
                case CoreAtmosGasses.Plasma:
                    return _filterPlasma;
            }

            return false;
        }

        private void FilterInteract(InteractionEvent interactionEvent, InteractionReference arg2)
        {
            GameObject filterView = Instantiate(_filterViewPrefab);
            filterView.GetComponent<FilterView>().Initialize(this);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcFilterGas(bool isFiltering, CoreAtmosGasses gas)
        {
            switch (gas)
            {
                case CoreAtmosGasses.Nitrogen:
                {
                    _filterNitrogen = isFiltering;
                    break;
                }

                case CoreAtmosGasses.Oxygen:
                {
                    _filterOxygen = isFiltering;
                    break;
                }

                case CoreAtmosGasses.CarbonDioxide:
                {
                    _filterCarbonDioxyde = isFiltering;
                    break;
                }

                case CoreAtmosGasses.Plasma:
                {
                    _filterPlasma = isFiltering;
                    break;
                }
            }
        }

        private void SyncFilterOxygen(bool oldValue, bool newValue, bool asServer)
        {
            OnUpdateFilterGas?.Invoke(newValue, CoreAtmosGasses.Oxygen);
        }

        private void SyncFilterNitrogen(bool oldValue, bool newValue, bool asServer)
        {
            OnUpdateFilterGas?.Invoke(newValue, CoreAtmosGasses.Nitrogen);
        }

        private void SyncFilterPlasma(bool oldValue, bool newValue, bool asServer)
        {
            OnUpdateFilterGas?.Invoke(newValue, CoreAtmosGasses.Plasma);
        }

        private void SyncFilterCarbonDioxyde(bool oldValue, bool newValue, bool asServer)
        {
            OnUpdateFilterGas?.Invoke(newValue, CoreAtmosGasses.CarbonDioxide);
        }

        private void SyncFilterActive(bool oldValue, bool newValue, bool asServer)
        {
            OnUpdateActive?.Invoke(newValue);
        }

        private void SyncFlux(float oldValue, float newValue, bool asServer)
        {
            OnUpdateFlux?.Invoke(newValue);
        }
    }
}
