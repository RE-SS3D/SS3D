using System.Collections.Generic;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Area;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Fixture realtime light + emissive visuals. Server computes lit mode; clients apply a SyncVar
    /// so they do not need the area registry or APC channel lookups the host uses.
    /// </summary>
    public class LightPower : NetworkActor
    {
        public enum FixtureVisual : byte
        {
            Off = 0,
            Normal = 1,
            Emergency = 2,
        }


        [SerializeField]
        private BasicPowerConsumer _consumer;
        [SerializeField]
        private Light _light;
        [SerializeField]
        private Light _fillLight;
        [SerializeField]
        private Renderer[] _emissiveRenderers;
        [SerializeField]
        private bool _respectDevBypass = true;
        [SerializeField]
        private bool _applyDepartmentalLightTint;
        [SerializeField]
        private LightFixtureCapability _fixtureCapability = LightFixtureCapability.NormalOnly;

        public LightFixtureCapability FixtureCapability => _fixtureCapability;
        [SerializeField]
        private float _emergencyIntensityMultiplier = 0.35f;
        [SerializeField]
        [Range(0.05f, 1f)]
        private float _emergencyRangeMultiplier = 0.45f;
        [SerializeField]
        private Color _emergencyTint = new Color(1f, 0.25f, 0.2f);
        [SerializeField]
        private LightFixtureBulb _bulb;
        [SerializeField]
        [Tooltip("How long the fixture's own cell keeps it on emergency light once its APC can't.")]
        private float _cellRunSeconds = 120f;
        [SerializeField]
        [Tooltip("How long a flat fixture cell takes to fill again while lit from mains.")]
        private float _cellRechargeSeconds = 60f;

        [SyncVar(OnChange = nameof(SyncFixtureVisual))]
        private FixtureVisual _fixtureVisual;

        /// <summary>The fixture's replicated lit mode (what clients render).</summary>
        public FixtureVisual Visual => _fixtureVisual;

        /// <summary>Server: the fixture's own battery, under the APC's cell.</summary>
        public FixtureEmergencyCell EmergencyCell => _cell ??= new FixtureEmergencyCell(_cellRunSeconds, _cellRechargeSeconds);

        /// <summary>Server: lit by its own cell because the APC can no longer light it.</summary>
        public bool IsOnEmergencyCell => _onCell;

        private FixtureEmergencyCell _cell;
        private bool _onCell;
        private float _lastCellTime = -1f;

        private float _poweredIntensity;
        private float _poweredFillIntensity;
        private float _poweredRange;
        private float _poweredFillRange;
        private Color _poweredLightColor = Color.white;
        private readonly List<Renderer> _emissiveRenderersToUpdate = new();
        private EmissiveMaterialSlots _emissiveSlots;
        private AreaId _areaId;
        private bool _hasArea;
        private bool _areaLightingSubscribed;
        private bool _electricityTickSubscribed;
        private bool _baselineCached;

        public override void OnStartServer()
        {
            base.OnStartServer();
            EnsureBaselineCached();
            SubscribeServerDrivers();
            RefreshVisuals();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            EnsureBaselineCached();
            ApplyFixtureVisual(_fixtureVisual);
        }

        protected override void OnDestroyed()
        {
            if (_consumer != null)
            {
                _consumer.OnPowerStatusUpdated -= HandlePowerStatusUpdated;
            }

            UnsubscribeServerDrivers();
            base.OnDestroyed();
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            // SubSystems may register after OnStartServer.
            if (!_areaLightingSubscribed || !_electricityTickSubscribed)
            {
                SubscribeServerDrivers();
            }
        }

        private void EnsureBaselineCached()
        {
            if (_baselineCached)
            {
                return;
            }

            if (_light != null)
            {
                _poweredIntensity = _light.intensity;
                _poweredRange = _light.range;
                _poweredLightColor = _light.color;
            }

            if (_fillLight != null)
            {
                _poweredFillIntensity = _fillLight.intensity;
                _poweredFillRange = _fillLight.range;
            }

            CacheEmissiveMaterials();
            _baselineCached = true;
        }

        private void CacheEmissiveMaterials()
        {
            _emissiveRenderersToUpdate.Clear();

            if (_emissiveRenderers == null || _emissiveRenderers.Length == 0)
            {
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.gameObject.name is "LightBulb" or "LightTube")
                    {
                        if (renderer != null)
                        {
                            _emissiveRenderersToUpdate.Add(renderer);
                        }
                    }
                }
            }
            else
            {
                foreach (Renderer renderer in _emissiveRenderers)
                {
                    if (renderer != null)
                    {
                        _emissiveRenderersToUpdate.Add(renderer);
                    }
                }
            }

            _emissiveSlots ??= new EmissiveMaterialSlots();
            _emissiveSlots.Cache(_emissiveRenderersToUpdate);
        }

        private void SubscribeServerDrivers()
        {
            if (!IsServer)
            {
                return;
            }

            if (_consumer != null)
            {
                _consumer.OnPowerStatusUpdated -= HandlePowerStatusUpdated;
                _consumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
            }

            TrySubscribeAreaLighting();
            TrySubscribeElectricityTick();
        }

        private void UnsubscribeServerDrivers()
        {
            UnsubscribeAreaLighting();
            UnsubscribeElectricityTick();
        }

        private void CacheAreaId()
        {
            _hasArea = false;
            if (_consumer?.TileObject == null || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            if (areaSubSystem.TryResolveAreaIdForDevice(_consumer.TileObject, out AreaId areaId))
            {
                _hasArea = true;
                _areaId = areaId;
                return;
            }

            // Host registry path (TryResolve already tries this; kept for clarity if cache lags).
            if (areaSubSystem.TryGetAreaForDevice(_consumer.TileObject, out AreaRecord record))
            {
                _hasArea = true;
                _areaId = record.Id;
            }
        }

        private void TrySubscribeAreaLighting()
        {
            if (_areaLightingSubscribed || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            areaSubSystem.OnAreaLightingStateChanged += HandleAreaLightingStateChanged;
            _areaLightingSubscribed = true;
            CacheAreaId();
        }

        private void UnsubscribeAreaLighting()
        {
            if (!_areaLightingSubscribed || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            areaSubSystem.OnAreaLightingStateChanged -= HandleAreaLightingStateChanged;
            _areaLightingSubscribed = false;
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus newStatus)
        {
            if (IsServer)
            {
                RefreshVisuals();
            }
        }

        private void HandleAreaLightingStateChanged(AreaId areaId, AreaLightingState state)
        {
            if (!IsServer)
            {
                return;
            }

            if (!_hasArea)
            {
                CacheAreaId();
            }

            if (_hasArea && _areaId == areaId)
            {
                RefreshVisuals();
            }
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

        private void HandleElectricityTick()
        {
            if (!IsServer)
            {
                return;
            }

            if (!_hasArea)
            {
                CacheAreaId();
            }

            UpdateEmergencyCell();
            RefreshVisuals();
        }

        /// <summary>Server, each electricity tick: charge the cell on mains, drain it while it carries the light.</summary>
        private void UpdateEmergencyCell()
        {
            float now = Time.time;
            float seconds = _lastCellTime < 0f ? 0f : now - _lastCellTime;
            _lastCellTime = now;

            if (_onCell)
            {
                EmergencyCell.Drain(seconds);
            }
            else if (_fixtureVisual == FixtureVisual.Normal)
            {
                EmergencyCell.OnMains(seconds);
            }
        }

        public static void RefreshAllFixtures()
        {
            foreach (LightPower fixture in FindObjectsByType<LightPower>(FindObjectsSortMode.None))
            {
                fixture.RefreshVisuals();
            }
        }

        /// <summary>
        /// Server: recompute and sync. Client: re-apply the last synced visual.
        /// </summary>
        public void RefreshVisuals()
        {
            EnsureBaselineCached();

            if (!IsServer)
            {
                ApplyFixtureVisual(_fixtureVisual);
                return;
            }

            FixtureVisual next = ComputeFixtureVisual();
            if (_fixtureVisual != next)
            {
                _fixtureVisual = next;
            }

            ApplyFixtureVisual(next);
        }

        private void SyncFixtureVisual(FixtureVisual _, FixtureVisual next, bool asServer)
        {
            if (!asServer)
            {
                EnsureBaselineCached();
                ApplyFixtureVisual(next);
            }
        }

        private FixtureVisual ComputeFixtureVisual()
        {
            if (_bulb == null)
            {
                TryGetComponent(out _bulb);
            }

            if (_bulb != null && !LightBulbRules.CanEmit(_bulb.State))
            {
                _onCell = false;
                return FixtureVisual.Off;
            }

            if (ShouldBeLit(out bool useEmergencyVisuals))
            {
                _onCell = false;
                return useEmergencyVisuals ? FixtureVisual.Emergency : FixtureVisual.Normal;
            }

            // The APC can't light it (cell flat, lighting shed or switched off at the APC, APC broken or gone):
            // the fixture's own cell takes over until it runs down. The wall switch is a choice, not a power cut.
            _onCell = _fixtureCapability == LightFixtureCapability.EmergencyCapable
                && EmergencyCell.CanLight
                && !IsSwitchedOffAtWall();
            return _onCell ? FixtureVisual.Emergency : FixtureVisual.Off;
        }

        private void ApplyFixtureVisual(FixtureVisual visual)
        {
            switch (visual)
            {
                case FixtureVisual.Normal:
                    TurnLightOnNormal();
                    break;
                case FixtureVisual.Emergency:
                    TurnLightOnEmergency();
                    break;
                default:
                    TurnLightOff();
                    break;
            }
        }

        private bool ShouldBeLit(out bool useEmergencyVisuals)
        {
            useEmergencyVisuals = false;

            if (_consumer == null)
            {
                return false;
            }

            CacheAreaId();

            if (!IsFixtureLightingChannelOpen())
            {
                return false;
            }

            AreaLightingState areaState = AreaLightingState.Normal;
            bool hasAreaContext = _hasArea;
            if (_hasArea
                && SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                && areaSubSystem.TryGetLightingState(_areaId, out areaState))
            {
                // Use derived area lighting state.
            }
            else if (_hasArea)
            {
                // Server registry path before first derive: treat as powered-area Normal.
                areaState = AreaLightingState.Normal;
            }
            else if (_respectDevBypass && LightingDevBypass.IsActive)
            {
                // Manual place / no APC flood: bypass still lights fixtures (grid-free glow).
                useEmergencyVisuals = false;
                return true;
            }
            else
            {
                return false;
            }

            if (areaState == AreaLightingState.Dark)
            {
                return false;
            }

            PowerStatus consumerStatus = _consumer.PowerStatus;
            if (_respectDevBypass && LightingDevBypass.IsActive)
            {
                consumerStatus = PowerStatus.Powered;
            }

            return AreaLightFixturePolicy.ShouldEmitLight(
                hasAreaContext,
                areaState,
                _fixtureCapability,
                consumerStatus,
                out useEmergencyVisuals);
        }

        private bool IsSwitchedOffAtWall()
        {
            return _hasArea
                && SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                && areaSubSystem.TryGetAreaLightingSwitchOn(_areaId, out bool on)
                && !on;
        }

        private bool IsFixtureLightingChannelOpen()
        {
            if (_consumer is not IElectricDevice device)
            {
                return _respectDevBypass && LightingDevBypass.IsActive;
            }

            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return _respectDevBypass && LightingDevBypass.IsActive;
            }

            if (areaSubSystem.TryGetEffectiveApcForDevice(device, out IApcChannelSource apc))
            {
                return PowerGate.IsChannelEnabled(_consumer.Channel, apc.Channels);
            }

            return _respectDevBypass && LightingDevBypass.IsActive;
        }

        private void TurnLightOnNormal()
        {
            Color emissionTint = Color.white;
            if (_applyDepartmentalLightTint
                && _hasArea
                && SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                && areaSubSystem.TryGetDepartmentalLightTint(_areaId, out Color tint))
            {
                emissionTint = tint;
            }

            ApplyLightState(_light, _poweredIntensity, _poweredRange, _poweredLightColor);
            ApplyLightState(_fillLight, _poweredFillIntensity, _poweredFillRange, _poweredLightColor);

            SetEmissiveState(1f, emissionTint);
        }

        private void TurnLightOnEmergency()
        {
            float emergencyIntensity = _poweredIntensity * _emergencyIntensityMultiplier;
            float emergencyRange = _poweredRange * _emergencyRangeMultiplier;
            ApplyLightState(_light, emergencyIntensity, emergencyRange, _emergencyTint);
            ApplyLightState(_fillLight, 0f, _poweredFillRange, _emergencyTint, enabled: false);

            SetEmissiveState(_emergencyIntensityMultiplier, _emergencyTint);
        }

        private void TurnLightOff()
        {
            ApplyLightState(_light, 0f, _poweredRange, _poweredLightColor, enabled: false);
            ApplyLightState(_fillLight, 0f, _poweredFillRange, _poweredLightColor, enabled: false);

            SetEmissiveState(0f, Color.black);
        }

        private static void ApplyLightState(Light light, float intensity, float range, Color color, bool enabled = true)
        {
            if (light == null)
            {
                return;
            }

            light.intensity = intensity;
            light.range = range;
            light.color = color;
            light.enabled = enabled && intensity > 0f;
        }

        /// <summary>Each emissive material's own powered values, lumin scaled and emission tinted.</summary>
        private void SetEmissiveState(float luminScale, Color emissionTint)
        {
            if (_emissiveSlots == null)
                CacheEmissiveMaterials();

            _emissiveSlots.Apply(luminScale, emissionTint);
        }
    }
}
