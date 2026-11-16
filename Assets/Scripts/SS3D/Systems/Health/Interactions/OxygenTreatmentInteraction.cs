using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    /// <summary>
    /// Help-intent oxygen delivery that reduces oxy debt (bypass, not a lung fix).
    /// </summary>
    public sealed class OxygenTreatmentInteraction : IInteraction, IInteractionTierProvider, ITargetedInteraction, IIntentRestrictedInteraction
    {
        public IntentType AllowedIntent => IntentType.Help;

        public string GetName(InteractionEvent interactionEvent) => "Apply oxygen";

        public string GetGenericName() => "Apply oxygen";

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Targeted;

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Examine);
        }

        public bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent)
        {
            InteractionEvent combined = targetEvent.WithSource(originEvent.Source);
            HumanHealthController health = MedicalInteractionUtility.ResolveHealth(combined);
            return health != null
                && CanInteract(combined)
                && MedicalInteractionUtility.TryResolveHeadZone(combined, health, out _);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!MedicalInteractionUtility.TryGetTreatmentItem<OxygenTankItemExtension>(interactionEvent.Source, out Item item)
                || !item.TryGetComponent(out GasTankItemExtension tank)
                || tank.IsEmpty)
            {
                return false;
            }

            HumanHealthController health = MedicalInteractionUtility.ResolveHealth(interactionEvent);
            if (health == null)
            {
                return false;
            }

            if (health.Snapshot.Pools.OxyDebt < HealthConstants.TreatmentOxyDebtThreshold)
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            HumanHealthController health = MedicalInteractionUtility.ResolveHealth(interactionEvent);
            if (health == null)
            {
                return false;
            }

            if (!MedicalInteractionUtility.TryResolveHeadZone(interactionEvent, health, out _))
            {
                return false;
            }

            if (!MedicalInteractionUtility.TryGetTreatmentItem<OxygenTankItemExtension>(interactionEvent.Source, out Item item)
                || !item.TryGetComponent(out GasTankItemExtension tank))
            {
                return false;
            }

            tank.ServerDraw(HealthConstants.OxygenTreatmentMoles);
            health.ApplyOxyRelief();
            return false;
        }
    }
}
