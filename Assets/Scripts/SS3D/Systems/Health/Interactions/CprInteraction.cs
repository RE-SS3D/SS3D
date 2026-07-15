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
    /// Help-intent chest CPR with a short windup that reduces oxy debt on critical patients.
    /// </summary>
    public sealed class CprInteraction : DelayedInteraction, IInteractionTierProvider, ITargetedInteraction, IIntentRestrictedInteraction
    {
        public CprInteraction()
        {
            Delay = HealthConstants.CprWindupSeconds;
            CheckInterval = 0.25f;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Examine);
        }

        public IntentType AllowedIntent => IntentType.Help;

        public InteractionTier GetTier(InteractionEvent interactionEvent) => InteractionTier.Targeted;

        public override string GetName(InteractionEvent interactionEvent) => "Perform CPR";

        public override string GetGenericName() => "Perform CPR";

        public bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent)
        {
            InteractionEvent combined = new(originEvent.Source, targetEvent.Target, targetEvent.Point, targetEvent.Normal);
            return CanInteract(combined)
                && MedicalInteractionUtility.TryResolveChestZone(combined, MedicalInteractionUtility.ResolveHealth(combined), out _);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source is not Hand hand || !hand.IsEmpty())
            {
                return false;
            }

            HumanHealthController health = MedicalInteractionUtility.ResolveHealth(interactionEvent);
            if (health == null)
            {
                return false;
            }

            HealthSnapshot snapshot = health.Snapshot;
            if (snapshot.State != HealthState.Critical
                && snapshot.State != HealthState.CardiacArrest
                && snapshot.Pools.OxyDebt < HealthConstants.CriticalOxyDebt)
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        [Server]
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            CaptureStartPosition(interactionEvent);
            StartCounter();
            return true;
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            HumanHealthController health = MedicalInteractionUtility.ResolveHealth(interactionEvent);
            if (health == null)
            {
                return;
            }

            if (!MedicalInteractionUtility.TryResolveChestZone(interactionEvent, health, out _))
            {
                return;
            }

            health.ApplyCpr();
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
