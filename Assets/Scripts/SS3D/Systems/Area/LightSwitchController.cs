using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Electricity;
using UnityEngine;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// Wall light switch that toggles area fixture lighting and dims when unpowered.
    /// </summary>
    [RequireComponent(typeof(BasicPowerConsumer))]
    [RequireComponent(typeof(ElectricDeviceAdjacencyConnector))]
    public sealed class LightSwitchController : InteractionTargetNetworkBehaviour, IToggleable
    {
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int LuminId = Shader.PropertyToID("_Lumin");

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;
        [SerializeField]
        private Renderer[] _emissiveRenderers;

        private readonly List<Material> _emissiveMaterials = new();
        private float _poweredLumin;
        private Color _poweredEmission;
        private AreaId _areaId;
        private bool _hasArea;
        private bool _lightsOn = true;
        private bool _areaEventsSubscribed;
        private bool _powerEventsSubscribed;
        private bool _electricityTickSubscribed;

        public bool GetState() => _lightsOn;

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new ToggleInteraction
                {
                    OnName = "Turn lights off",
                    OffName = "Turn lights on",
                    CanInteractCallback = _ => IsPowered(),
                },
            };
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Initialize();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Initialize();
        }

        protected override void OnDestroyed()
        {
            UnsubscribeAreaEvents();
            UnsubscribePowerEvents();
            UnsubscribeElectricityTick();
            base.OnDestroyed();
        }

        [Server]
        public void Toggle()
        {
            if (!IsPowered() || !_hasArea || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            if (areaSubSystem.ToggleAreaLightingSwitch(_areaId))
            {
                SyncSwitchState();
            }
        }

        private void Initialize()
        {
            if (_powerConsumer == null)
            {
                TryGetComponent(out _powerConsumer);
            }

            CacheEmissiveMaterials();
            CacheArea();
            TrySubscribeAreaEvents();
            TrySubscribePowerEvents();
            TrySubscribeElectricityTick();
            SyncSwitchState();
            RefreshVisuals();
        }

        private void CacheEmissiveMaterials()
        {
            _emissiveMaterials.Clear();

            Renderer[] renderers = _emissiveRenderers != null && _emissiveRenderers.Length > 0
                ? _emissiveRenderers
                : GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (!material.HasProperty(LuminId) && !material.HasProperty(EmissionColorId))
                    {
                        continue;
                    }

                    _emissiveMaterials.Add(material);
                }
            }

            if (_emissiveMaterials.Count == 0)
            {
                return;
            }

            Material referenceMaterial = _emissiveMaterials[0];
            if (referenceMaterial.HasProperty(LuminId))
            {
                _poweredLumin = referenceMaterial.GetFloat(LuminId);
            }

            if (referenceMaterial.HasProperty(EmissionColorId))
            {
                _poweredEmission = referenceMaterial.GetColor(EmissionColorId);
            }
        }

        private void CacheArea()
        {
            _hasArea = false;
            PlacedTileObject tileObject = GetComponent<PlacedTileObject>();
            if (tileObject == null || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            if (!areaSubSystem.TryGetAreaForDevice(tileObject, out AreaRecord record))
            {
                return;
            }

            _hasArea = true;
            _areaId = record.Id;
        }

        private void SyncSwitchState()
        {
            if (!_hasArea || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            if (areaSubSystem.TryGetAreaLightingSwitchOn(_areaId, out bool on))
            {
                _lightsOn = on;
            }
        }

        private void TrySubscribeAreaEvents()
        {
            if (_areaEventsSubscribed || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            if (areaSubSystem.IsSetUp)
            {
                areaSubSystem.OnAreaLightingSwitchChanged += HandleAreaLightingSwitchChanged;
                _areaEventsSubscribed = true;
                return;
            }

            areaSubSystem.OnSystemSetUp += HandleAreaSystemSetup;
        }

        private void HandleAreaSystemSetup()
        {
            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            areaSubSystem.OnSystemSetUp -= HandleAreaSystemSetup;
            if (!_areaEventsSubscribed)
            {
                areaSubSystem.OnAreaLightingSwitchChanged += HandleAreaLightingSwitchChanged;
                _areaEventsSubscribed = true;
            }

            CacheArea();
            SyncSwitchState();
            RefreshVisuals();
        }

        private void UnsubscribeAreaEvents()
        {
            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            areaSubSystem.OnSystemSetUp -= HandleAreaSystemSetup;
            if (_areaEventsSubscribed)
            {
                areaSubSystem.OnAreaLightingSwitchChanged -= HandleAreaLightingSwitchChanged;
                _areaEventsSubscribed = false;
            }
        }

        private void TrySubscribePowerEvents()
        {
            if (_powerEventsSubscribed || _powerConsumer == null)
            {
                return;
            }

            _powerConsumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
            _powerEventsSubscribed = true;
        }

        private void UnsubscribePowerEvents()
        {
            if (!_powerEventsSubscribed || _powerConsumer == null)
            {
                return;
            }

            _powerConsumer.OnPowerStatusUpdated -= HandlePowerStatusUpdated;
            _powerEventsSubscribed = false;
        }

        private void TrySubscribeElectricityTick()
        {
            if (_electricityTickSubscribed || !SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            if (electricitySubSystem.IsSetUp)
            {
                electricitySubSystem.OnTick += HandleElectricityTick;
                _electricityTickSubscribed = true;
                return;
            }

            electricitySubSystem.OnSystemSetUp += HandleElectricitySystemSetup;
        }

        private void HandleElectricitySystemSetup()
        {
            if (!SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            electricitySubSystem.OnSystemSetUp -= HandleElectricitySystemSetup;
            if (!_electricityTickSubscribed)
            {
                electricitySubSystem.OnTick += HandleElectricityTick;
                _electricityTickSubscribed = true;
            }

            RefreshVisuals();
        }

        private void UnsubscribeElectricityTick()
        {
            if (!SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            electricitySubSystem.OnSystemSetUp -= HandleElectricitySystemSetup;
            if (_electricityTickSubscribed)
            {
                electricitySubSystem.OnTick -= HandleElectricityTick;
                _electricityTickSubscribed = false;
            }
        }

        private void HandleAreaLightingSwitchChanged(AreaId areaId, bool on)
        {
            if (_hasArea && _areaId == areaId)
            {
                _lightsOn = on;
                RefreshVisuals();
            }
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus newStatus)
        {
            RefreshVisuals();
        }

        private void HandleElectricityTick()
        {
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            bool emit = IsPowered() && _lightsOn;
            foreach (Material material in _emissiveMaterials)
            {
                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty(LuminId))
                {
                    material.SetFloat(LuminId, emit ? _poweredLumin : 0f);
                }

                if (material.HasProperty(EmissionColorId))
                {
                    material.SetColor(EmissionColorId, emit ? _poweredEmission : Color.black);
                }
            }
        }

        private bool IsPowered()
        {
            return PowerGate.IsEffectivelyPowered(_powerConsumer, NullConsumerPolicy.Deny);
        }
    }
}
