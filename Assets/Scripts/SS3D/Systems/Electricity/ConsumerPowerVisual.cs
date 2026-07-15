using System;
using System.Collections.Generic;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Dims emissive materials and optional panel indicators when a power consumer is inactive.
    /// </summary>
    public class ConsumerPowerVisual : MonoBehaviour
    {
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int LuminId = Shader.PropertyToID("_Lumin");

        [Serializable]
        struct EmissiveSlot
        {
            public Material Material;
            public float PoweredLumin;
            public Color PoweredEmission;
        }

        [Serializable]
        struct PanelIndicatorSlot
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public Color PoweredColor;
        }

        [SerializeField]
        private MonoBehaviour _consumerBehaviour;
        [SerializeField]
        private Renderer[] _renderers;
        [SerializeField]
        private PanelIndicatorSlot[] _panelIndicators;

        private IPowerConsumer _consumer;
        private readonly List<EmissiveSlot> _emissiveSlots = new();
        private bool _electricityTickSubscribed;

        private void Awake()
        {
            _consumer = ResolveConsumer();
        }

        private void Start()
        {
            if (_consumer == null)
            {
                _consumer = ResolveConsumer();
            }

            if (_consumer is BasicPowerConsumer basicConsumer)
            {
                basicConsumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
            }
            else if (_consumer is MachinePowerConsumer machineConsumer)
            {
                machineConsumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
            }

            CacheVisuals();
            TrySubscribeElectricityTick();
            RefreshVisuals();
        }

        private void OnDestroy()
        {
            if (_consumer is BasicPowerConsumer basicConsumer)
            {
                basicConsumer.OnPowerStatusUpdated -= HandlePowerStatusUpdated;
            }
            else if (_consumer is MachinePowerConsumer machineConsumer)
            {
                machineConsumer.OnPowerStatusUpdated -= HandlePowerStatusUpdated;
            }

            UnsubscribeElectricityTick();
        }

        private IPowerConsumer ResolveConsumer()
        {
            if (_consumerBehaviour is IPowerConsumer behaviourConsumer)
            {
                return behaviourConsumer;
            }

            if (TryGetComponent(out BasicPowerConsumer basicConsumer))
            {
                return basicConsumer;
            }

            if (TryGetComponent(out MachinePowerConsumer machineConsumer))
            {
                return machineConsumer;
            }

            return null;
        }

        private void CacheVisuals()
        {
            _emissiveSlots.Clear();

            Renderer[] renderers = _renderers != null && _renderers.Length > 0
                ? _renderers
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

                    float lumin = material.HasProperty(LuminId) ? material.GetFloat(LuminId) : 0f;
                    Color emission = material.HasProperty(EmissionColorId)
                        ? material.GetColor(EmissionColorId)
                        : Color.black;

                    _emissiveSlots.Add(new EmissiveSlot
                    {
                        Material = material,
                        PoweredLumin = lumin,
                        PoweredEmission = emission,
                    });
                }
            }

            if (_panelIndicators == null)
            {
                return;
            }

            for (int i = 0; i < _panelIndicators.Length; i++)
            {
                PanelIndicatorSlot slot = _panelIndicators[i];
                if (slot.Renderer == null || slot.MaterialIndex < 0)
                {
                    continue;
                }

                Material[] materials = slot.Renderer.materials;
                if (slot.MaterialIndex >= materials.Length)
                {
                    continue;
                }

                slot.PoweredColor = materials[slot.MaterialIndex].color;
                _panelIndicators[i] = slot;
            }
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus newStatus)
        {
            RefreshVisuals();
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
            if (ShouldShowPowered())
            {
                SetEmissiveState(true);
                SetPanelIndicators(true);
            }
            else
            {
                SetEmissiveState(false);
                SetPanelIndicators(false);
            }
        }

        private bool ShouldShowPowered()
        {
            return PowerGate.IsEffectivelyPowered(_consumer, NullConsumerPolicy.Deny);
        }

        private void SetEmissiveState(bool powered)
        {
            foreach (EmissiveSlot slot in _emissiveSlots)
            {
                if (slot.Material == null)
                {
                    continue;
                }

                if (slot.Material.HasProperty(LuminId))
                {
                    slot.Material.SetFloat(LuminId, powered ? slot.PoweredLumin : 0f);
                }

                if (slot.Material.HasProperty(EmissionColorId))
                {
                    slot.Material.SetColor(EmissionColorId, powered ? slot.PoweredEmission : Color.black);
                }
            }
        }

        private void SetPanelIndicators(bool powered)
        {
            if (_panelIndicators == null || powered)
            {
                return;
            }

            foreach (PanelIndicatorSlot slot in _panelIndicators)
            {
                if (slot.Renderer == null || slot.MaterialIndex < 0)
                {
                    continue;
                }

                Material[] materials = slot.Renderer.materials;
                if (slot.MaterialIndex >= materials.Length)
                {
                    continue;
                }

                materials[slot.MaterialIndex].color = Color.black;
            }
        }
    }
}
