using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Electricity;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// Wall light switch that toggles area fixture lighting and dims when unpowered.
    /// </summary>
    [RequireComponent(typeof(BasicPowerConsumer))]
    [RequireComponent(typeof(ElectricDeviceAdjacencyConnector))]
    public sealed class LightSwitchController : InteractionTargetNetworkBehaviour, IToggleable
    {
        private const float ClickVolume = 0.5f;
        private const float ClickMinRange = 1f;
        private const float ClickMaxRange = 8f;

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;
        [SerializeField]
        private Renderer[] _emissiveRenderers;

        private EmissiveMaterialSlots _emissiveSlots;
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
                PlayClick();
            }
        }

        [Server]
        private void PlayClick()
        {
            if (SubSystems.TryGet(out AudioSubSystem audio))
            {
                audio.PlayAudioSource(AudioType.Sfx, LightSwitchAudioTrackIds.Click, transform.position, null,
                    false, ClickVolume, 1f, ClickMinRange, ClickMaxRange);
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

        private void Update()
        {
            if (!_areaEventsSubscribed)
            {
                TrySubscribeAreaEvents();
            }

            if (!_electricityTickSubscribed)
            {
                TrySubscribeElectricityTick();
            }
        }

        private void CacheEmissiveMaterials()
        {
            _emissiveSlots ??= new EmissiveMaterialSlots();
            _emissiveSlots.Cache(_emissiveRenderers != null && _emissiveRenderers.Length > 0
                ? _emissiveRenderers
                : GetComponentsInChildren<Renderer>(true));
        }

        private void CacheArea()
        {
            _hasArea = false;
            PlacedTileObject tileObject = GetComponent<PlacedTileObject>();
            if (tileObject == null || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            if (!areaSubSystem.TryResolveAreaIdForDevice(tileObject, out AreaId areaId))
            {
                return;
            }

            _hasArea = true;
            _areaId = areaId;
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

            areaSubSystem.OnAreaLightingSwitchChanged += HandleAreaLightingSwitchChanged;
            areaSubSystem.FloorVisualCache.OnDirty += HandleFloorVisualCacheDirty;
            _areaEventsSubscribed = true;
            CacheArea();
            SyncSwitchState();
        }

        private void HandleFloorVisualCacheDirty()
        {
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

            if (_areaEventsSubscribed)
            {
                areaSubSystem.OnAreaLightingSwitchChanged -= HandleAreaLightingSwitchChanged;
                areaSubSystem.FloorVisualCache.OnDirty -= HandleFloorVisualCacheDirty;
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

            electricitySubSystem.OnTick += HandleElectricityTick;
            _electricityTickSubscribed = true;
        }

        private void UnsubscribeElectricityTick()
        {
            if (!_electricityTickSubscribed || !SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            electricitySubSystem.OnTick -= HandleElectricityTick;
            _electricityTickSubscribed = false;
        }

        private void HandleAreaLightingSwitchChanged(AreaId areaId, bool on)
        {
            if (!_hasArea)
            {
                CacheArea();
            }

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
            if (_emissiveSlots == null)
                CacheEmissiveMaterials();

            _emissiveSlots.ApplyPowered(IsPowered() && _lightsOn);
        }

        private bool IsPowered()
        {
            return PowerGate.IsEffectivelyPowered(_powerConsumer, NullConsumerPolicy.Deny);
        }
    }
}
