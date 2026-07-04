using System.Collections.Generic;
using UnityEngine;

namespace System.Electricity
{
    /// <summary>
    /// Toggles a fixture's realtime light and emissive mesh visuals based on power status.
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

        private float _poweredIntensity;
        private float _poweredLumin;
        private Color _poweredEmission;
        private readonly List<Material> _emissiveMaterials = new();

        private void Start()
        {
            if (_consumer != null)
            {
                _consumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
            }

            if (_light != null)
            {
                _poweredIntensity = _light.intensity;
            }

            CacheEmissiveMaterials();
            RefreshVisuals();
        }

        private void OnDestroy()
        {
            if (_consumer != null)
            {
                _consumer.OnPowerStatusUpdated -= HandlePowerStatusUpdated;
            }
        }

        private void CacheEmissiveMaterials()
        {
            _emissiveMaterials.Clear();

            if (_emissiveRenderers == null || _emissiveRenderers.Length == 0)
            {
                foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.gameObject.name is "LightBulb" or "LightTube")
                    {
                        _emissiveMaterials.Add(renderer.material);
                    }
                }
            }
            else
            {
                foreach (var renderer in _emissiveRenderers)
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

            var referenceMaterial = _emissiveMaterials[0];
            if (referenceMaterial.HasProperty(LuminId))
            {
                _poweredLumin = referenceMaterial.GetFloat(LuminId);
            }

            if (referenceMaterial.HasProperty(EmissionColorId))
            {
                _poweredEmission = referenceMaterial.GetColor(EmissionColorId);
            }
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus newStatus)
        {
            RefreshVisuals();
        }

        public void RefreshVisuals()
        {
            if (ShouldBeLit())
            {
                TurnLightOn();
            }
            else
            {
                TurnLightOff();
            }
        }

        private bool ShouldBeLit()
        {
            if (_respectDevBypass && LightingDevBypass.IsActive)
            {
                return true;
            }

            return _consumer != null && _consumer.PowerStatus == PowerStatus.Powered;
        }

        private void TurnLightOn()
        {
            if (_light != null)
            {
                _light.intensity = _poweredIntensity;
                _light.enabled = true;
            }

            SetEmissiveState(_poweredLumin, _poweredEmission);
        }

        private void TurnLightOff()
        {
            if (_light != null)
            {
                _light.intensity = 0f;
            }

            SetEmissiveState(0f, Color.black);
        }

        private void SetEmissiveState(float lumin, Color emissionColor)
        {
            foreach (var material in _emissiveMaterials)
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
