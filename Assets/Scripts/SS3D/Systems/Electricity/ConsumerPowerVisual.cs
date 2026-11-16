using System;
using System.Collections.Generic;
using SS3D.Core;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Dims emissive materials and optional panel indicators when a power consumer is inactive.
    /// Driven by <see cref="IPowerConsumer"/> status changes (and a one-shot refresh when electricity
    /// becomes ready) — not per-tick <see cref="ElectricitySubSystem.OnTick"/>, which is too expensive
    /// at SS13 door counts when power is steady.
    /// </summary>
    public class ConsumerPowerVisual : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        [Serializable]
        struct PanelIndicatorSlot
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public Color PoweredColor;
            /// <summary>Determined from the target shared material once — used for MPB updates.</summary>
            [NonSerialized]
            public int ColorPropertyId;
        }

        [SerializeField]
        private MonoBehaviour _consumerBehaviour;
        [SerializeField]
        private Renderer[] _renderers;
        [SerializeField]
        private PanelIndicatorSlot[] _panelIndicators;

        private IPowerConsumer _consumer;
        private EmissiveMaterialSlots _emissiveSlots;
        private bool _waitingForElectricityReady;
        private bool _hasAppliedPowered;
        private bool _lastShownPowered;

        private MaterialPropertyBlock _panelPropertyBlock;

        private void Awake()
        {
            _consumer = ResolveConsumer();

            // Unity disallows `new MaterialPropertyBlock()` in instance field initializers.
            _emissiveSlots = new EmissiveMaterialSlots();
            _panelPropertyBlock = new MaterialPropertyBlock();
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
            TryBindElectricityReady();
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

            UnbindElectricityReady();
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
            Renderer[] renderers = _renderers != null && _renderers.Length > 0
                ? _renderers
                : GetComponentsInChildren<Renderer>(true);
            _emissiveSlots ??= new EmissiveMaterialSlots();
            _emissiveSlots.Cache(renderers);

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

                Material[] sharedMaterials = slot.Renderer.sharedMaterials;
                if (slot.MaterialIndex >= sharedMaterials.Length)
                {
                    continue;
                }

                Material targetMaterial = sharedMaterials[slot.MaterialIndex];
                if (targetMaterial == null)
                {
                    continue;
                }

                // Material.color is the visible base-color alias for the shader in use.
                slot.PoweredColor = targetMaterial.color;

                // Pick a color property that exists on the targeted shader; use MPB so we don't instantiate materials.
                if (targetMaterial.HasProperty(BaseColorId))
                {
                    slot.ColorPropertyId = BaseColorId;
                }
                else if (targetMaterial.HasProperty(ColorId))
                {
                    slot.ColorPropertyId = ColorId;
                }
                else
                {
                    // No known base-color property; skip updates for this slot.
                    continue;
                }

                _panelIndicators[i] = slot;
            }
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus newStatus)
        {
            RefreshVisuals();
        }

        private void TryBindElectricityReady()
        {
            if (_waitingForElectricityReady || !SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            if (electricitySubSystem.IsReady)
            {
                return;
            }

            electricitySubSystem.WhenReady += HandleElectricitySystemSetup;
            _waitingForElectricityReady = true;
        }

        private void HandleElectricitySystemSetup()
        {
            if (!SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            electricitySubSystem.WhenReady -= HandleElectricitySystemSetup;
            _waitingForElectricityReady = false;
            RefreshVisuals();
        }

        private void UnbindElectricityReady()
        {
            if (!_waitingForElectricityReady || !SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                return;
            }

            electricitySubSystem.WhenReady -= HandleElectricitySystemSetup;
            _waitingForElectricityReady = false;
        }

        public void RefreshVisuals()
        {
            bool powered = ShouldShowPowered();
            if (_hasAppliedPowered && _lastShownPowered == powered)
            {
                return;
            }

            _hasAppliedPowered = true;
            _lastShownPowered = powered;

            if (powered)
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
            // Prefer simulation PowerStatus: area ticks already apply channel gating before
            // assigning Powered/Inactive. Avoids per-tick APC lookups on hundreds of airlocks.
            return PowerGate.IsPowered(_consumer, NullConsumerPolicy.Deny);
        }

        private void SetEmissiveState(bool powered)
        {
            _emissiveSlots.ApplyPowered(powered);
        }

        private void SetPanelIndicators(bool powered)
        {
            if (_panelIndicators == null)
            {
                return;
            }

            for (int i = 0; i < _panelIndicators.Length; i++)
            {
                PanelIndicatorSlot slot = _panelIndicators[i];
                if (slot.Renderer == null)
                {
                    continue;
                }

                if (slot.ColorPropertyId == 0)
                {
                    continue;
                }

                slot.Renderer.GetPropertyBlock(_panelPropertyBlock, slot.MaterialIndex);
                _panelPropertyBlock.SetColor(slot.ColorPropertyId, powered ? slot.PoweredColor : Color.black);
                slot.Renderer.SetPropertyBlock(_panelPropertyBlock, slot.MaterialIndex);
            }
        }

    }
}
