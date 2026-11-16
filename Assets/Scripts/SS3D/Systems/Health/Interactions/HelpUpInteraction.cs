using FishNet.Object;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Health.Interactions
{
    /// <summary>
    /// Help-intent, empty-handed: pulls a Downed body back onto its feet once its shock is no higher than the Downed line
    /// and it has a leg to stand on (health.md §4 "Getting back up"). The body is unsteady for a few seconds after.
    /// </summary>
    public sealed class HelpUpInteraction : DelayedInteraction, IInteractionTierProvider, IIntentRestrictedInteraction
    {
        public HelpUpInteraction()
        {
            Delay = HealthConstants.HelpUpSeconds;
            CheckInterval = 0.25f;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Examine);
        }

        public IntentType AllowedIntent => IntentType.Help;

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Targeted;

        public override string GetName(InteractionEvent interactionEvent) => "Help up";

        public override string GetGenericName() => "Help up";

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source is not Hand hand || !hand.IsEmpty())
            {
                return false;
            }

            HumanHealthController health = MedicalInteractionUtility.ResolveHealth(interactionEvent);
            if (health == null || hand.GetComponentInParent<HumanHealthController>() == health)
            {
                return false;
            }

            return ShockSimulation.CanBeHelpedUp(health.Snapshot) && InteractionExtensions.RangeCheck(interactionEvent);
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            MedicalInteractionUtility.ResolveHealth(interactionEvent)?.ServerHelpUp();
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
