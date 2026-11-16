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
    /// Help-intent, zone-targeted burn dressing that heals burn damage on one zone.
    /// </summary>
    public sealed class BurnDressingInteraction : IInteraction, IInteractionTierProvider, ITargetedInteraction, IIntentRestrictedInteraction
    {
        public IntentType AllowedIntent => IntentType.Help;

        public string GetName(InteractionEvent interactionEvent) => "Apply burn dressing";

        public string GetGenericName() => "Apply burn dressing";

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Targeted;

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Examine);
        }

        public bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent)
        {
            InteractionEvent combined = targetEvent.WithSource(originEvent.Source);
            if (!CanInteract(combined))
            {
                return false;
            }

            HumanHealthController health = MedicalInteractionUtility.ResolveHealth(combined);
            if (health == null)
            {
                return false;
            }

            if (!MedicalInteractionUtility.TryResolveZone(combined, health, out BodyZone zone))
            {
                return false;
            }

            return health.DebugDetail.GetZone(zone).Burn > 0f;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!MedicalInteractionUtility.TryGetTreatmentItem<BurnDressingItemExtension>(interactionEvent.Source, out _))
            {
                return false;
            }

            HumanHealthController health = MedicalInteractionUtility.ResolveHealth(interactionEvent);
            if (health == null || health.Snapshot.WorstZoneBurn <= 0f)
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

            if (!MedicalInteractionUtility.TryResolveZone(interactionEvent, health, out BodyZone zone))
            {
                return false;
            }

            health.ApplyTreatment(zone, burnHeal: HealthConstants.BurnDressingHealAmount);

            if (MedicalInteractionUtility.TryGetTreatmentItem<BurnDressingItemExtension>(interactionEvent.Source, out Item item))
            {
                MedicalInteractionUtility.ConsumeTreatmentItem(item);
            }

            return false;
        }
    }
}
