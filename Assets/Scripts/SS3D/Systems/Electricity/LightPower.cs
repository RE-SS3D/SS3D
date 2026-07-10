using System.Collections.Generic;
using SS3D.Core;
using SS3D.Systems.Area;
using UnityEngine;

namespace System.Electricity
{
    /// <summary>
    /// Toggles a fixture's realtime light and emissive mesh visuals based on power and area lighting state.
    /// </summary>
    public class LightPower : MonoBehaviour
    {
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int LuminId = Shader.PropertyToID("_Lumin");

        [SerializeField]
        private BasicPowerConsumer _consumer;
        [SerializeField]
        private Light _light;
        [SerializeField]
        private Renderer[] _emissiveRenderers;
        [SerializeField]
        private bool _respectDevBypass = true;
        [SerializeField]
        private LightFixtureCapability _fixtureCapability = LightFixtureCapability.NormalOnly;

        public LightFixtureCapability FixtureCapability => _fixtureCapability;
        [SerializeField]
        private float _emergencyIntensityMultiplier = 0.35f;
        [SerializeField]
        private Color _emergencyTint = new Color(1f, 0.25f, 0.2f);

        private float _poweredIntensity;
        private Color _poweredLightColor = Color.white;
        private float _poweredLumin;
        private Color _poweredEmission;
        private readonly List<Material> _emissiveMaterials = new();
        private AreaId _areaId;
        private bool _hasArea;
        private bool _areaLightingSubscribed;
        private bool _electricityTickSubscribed;

        private void Start()
        {
            if (_consumer != null)
            {
                _consumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
            }

            if (_light != null)
            {
                _poweredIntensity = _light.intensity;
                _poweredLightColor = _light.color;
            }

            CacheEmissiveMaterials();
            CacheAreaId();
            TrySubscribeAreaLighting();
            TrySubscribeElectricityTick();
            RefreshVisuals();
        }

        private void OnDestroy()
        {
            if (_consumer != null)
            {
                _consumer.OnPowerStatusUpdated -= HandlePowerStatusUpdated;
            }

            UnsubscribeAreaLighting();
            UnsubscribeElectricityTick();
        }

        private void CacheEmissiveMaterials()
        {
            _emissiveMaterials.Clear();

            if (_emissiveRenderers == null || _emissiveRenderers.Length == 0)
            {
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.gameObject.name is "LightBulb" or "LightTube")
                    {
                        _emissiveMaterials.Add(renderer.material);
                    }
                }
            }
            else
            {
                foreach (Renderer renderer in _emissiveRenderers)
                {
                    if (renderer != null)
                    {
                        _emissiveMaterials.Add(renderer.material);
                    }
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

        private void CacheAreaId()
        {
            _hasArea = false;
            if (_consumer?.TileObject == null || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            if (!areaSubSystem.TryGetAreaForDevice(_consumer.TileObject, out AreaRecord record))
            {
                return;
            }

            _hasArea = true;
            _areaId = record.Id;
        }

        private void TrySubscribeAreaLighting()
        {
            if (_areaLightingSubscribed || !SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            if (areaSubSystem.IsSetUp)
            {
                areaSubSystem.OnAreaLightingStateChanged += HandleAreaLightingStateChanged;
                _areaLightingSubscribed = true;
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
            if (!_areaLightingSubscribed)
            {
                areaSubSystem.OnAreaLightingStateChanged += HandleAreaLightingStateChanged;
                _areaLightingSubscribed = true;
            }

            CacheAreaId();
            RefreshVisuals();
        }

        private void UnsubscribeAreaLighting()
        {
            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                return;
            }

            areaSubSystem.OnSystemSetUp -= HandleAreaSystemSetup;
            if (_areaLightingSubscribed)
            {
                areaSubSystem.OnAreaLightingStateChanged -= HandleAreaLightingStateChanged;
                _areaLightingSubscribed = false;
            }
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus newStatus)
        {
            RefreshVisuals();
        }

        private void HandleAreaLightingStateChanged(AreaId areaId, AreaLightingState state)
        {
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

        private void HandleElectricityTick()
        {
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            if (ShouldBeLit(out bool useEmergencyVisuals))
            {
                if (useEmergencyVisuals)
                {
                    TurnLightOnEmergency();
                }
                else
                {
                    TurnLightOnNormal();
                }
            }
            else
            {
                TurnLightOff();
            }
        }

        private bool ShouldBeLit(out bool useEmergencyVisuals)
        {
            useEmergencyVisuals = false;

            PowerStatus consumerStatus = _consumer != null ? _consumer.PowerStatus : PowerStatus.Inactive;
            if (!IsConsumerApcChannelEnabled())
            {
                consumerStatus = PowerStatus.Inactive;
            }
            else if (_respectDevBypass && LightingDevBypass.IsActive)
            {
                consumerStatus = PowerStatus.Powered;
            }

            AreaLightingState areaState = AreaLightingState.Dark;
            if (_hasArea
                && SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                && areaSubSystem.TryGetLightingState(_areaId, out areaState))
            {
                // Use derived area lighting state.
            }
            else if (_hasArea)
            {
                areaState = AreaLightingState.Normal;
            }

            return AreaLightFixturePolicy.ShouldEmitLight(
                _hasArea,
                areaState,
                _fixtureCapability,
                consumerStatus,
                out useEmergencyVisuals);
        }

        private bool IsConsumerApcChannelEnabled()
        {
            if (_consumer is not IElectricDevice device
                || !SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                || !areaSubSystem.TryGetEffectiveApcForDevice(device, out IApcChannelSource apc))
            {
                return true;
            }

            return AreaApcPowerDistribution.IsChannelEnabled(_consumer.Channel, apc.Channels);
        }

        private void TurnLightOnNormal()
        {
            Color emission = _poweredEmission;
            if (_hasArea
                && SubSystems.TryGet(out AreaSubSystem areaSubSystem)
                && _consumer?.TileObject != null
                && areaSubSystem.TryGetAreaForDevice(_consumer.TileObject, out AreaRecord record)
                && record.HasDepartmentalLightTint)
            {
                emission = MultiplyColor(_poweredEmission, record.DepartmentalLightTint);
            }

            if (_light != null)
            {
                _light.intensity = _poweredIntensity;
                _light.color = _poweredLightColor;
                _light.enabled = true;
            }

            SetEmissiveState(_poweredLumin, emission);
        }

        private void TurnLightOnEmergency()
        {
            if (_light != null)
            {
                _light.intensity = _poweredIntensity * _emergencyIntensityMultiplier;
                _light.color = _emergencyTint;
                _light.enabled = true;
            }

            SetEmissiveState(_poweredLumin * _emergencyIntensityMultiplier, MultiplyColor(_poweredEmission, _emergencyTint));
        }

        private void TurnLightOff()
        {
            if (_light != null)
            {
                _light.intensity = 0f;
                _light.enabled = false;
            }

            SetEmissiveState(0f, Color.black);
        }

        private static Color MultiplyColor(Color left, Color right)
        {
            return new Color(left.r * right.r, left.g * right.g, left.b * right.b, left.a * right.a);
        }

        private void SetEmissiveState(float lumin, Color emissionColor)
        {
            foreach (Material material in _emissiveMaterials)
            {
                if (material.HasProperty(LuminId))
                {
                    material.SetFloat(LuminId, lumin);
                }

                if (material.HasProperty(EmissionColorId))
                {
                    material.SetColor(EmissionColorId, emissionColor);
                }
            }
        }
    }
}
