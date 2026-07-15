using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    /// <summary>
    /// Help-intent antitoxin injection that reduces systemic toxin concentration.
    /// </summary>
    public sealed class AntitoxinInteraction : IInteraction, IInteractionTierProvider, ITargetedInteraction, IIntentRestrictedInteraction
    {
        public IntentType AllowedIntent => IntentType.Help;

        public string GetName(InteractionEvent interactionEvent) => "Inject antitoxin";

        public string GetGenericName() => "Inject antitoxin";

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Targeted;

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Examine);
        }

        public bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent)
        {
            InteractionEvent combined = new(originEvent.Source, targetEvent.Target, targetEvent.Point, targetEvent.Normal);
            HumanHealthController health = MedicalInteractionUtility.ResolveHealth(combined);
            return health != null
                && CanInteract(combined)
                && MedicalInteractionUtility.TryResolveChestZone(combined, health, out _);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!MedicalInteractionUtility.TryGetTreatmentItem<AntitoxinItemExtension>(interactionEvent.Source, out _))
            {
                return false;
            }

            HumanHealthController health = MedicalInteractionUtility.ResolveHealth(interactionEvent);
            if (health == null || health.Snapshot.Pools.ToxinConcentration <= 0f)
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

            if (!MedicalInteractionUtility.TryResolveChestZone(interactionEvent, health, out _))
            {
                return false;
            }

            health.ApplyAntitoxin();
            return false;
        }
    }
}
