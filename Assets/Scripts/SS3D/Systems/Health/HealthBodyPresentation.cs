using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Server: turns a health snapshot into body presentation intent on <see cref="Ragdoll"/> (the only writer health
    /// has), and drops held items when hands stop working. Death is owned by <see cref="Entities.Human.Kill"/> →
    /// <see cref="Ragdoll.ServerDeathRagdoll"/> and is ignored here.
    /// </summary>
    public sealed class HealthBodyPresentation
    {
        private readonly Ragdoll _ragdoll;

        /// <summary>Health put the body on the floor (Collapsed or Downed), so health also stands it back up.</summary>
        private bool _healthOwnsFloor;

        public HealthBodyPresentation(Ragdoll ragdoll)
        {
            _ragdoll = ragdoll;
        }

        public void Apply(HealthSnapshot snapshot)
        {
            if (_ragdoll == null || snapshot.State == HealthState.Dead)
            {
                return;
            }

            BodyPresentationState intent = BodyPresentationIntent.FromSnapshot(snapshot);
            if (intent == BodyPresentationState.Dead)
            {
                return;
            }

            // Collapsed, Downed or Locomotion — never fight Kill()'s Dead write.
            if (intent == BodyPresentationState.Collapsed)
            {
                if (_ragdoll.Presentation != BodyPresentationState.Collapsed)
                {
                    _ragdoll.ServerSetPresentation(BodyPresentationState.Collapsed);
                }

                _healthOwnsFloor = true;
                return;
            }

            // A timed combat knockdown (taser, shove, blast, sprint trip) runs out first; its recovery comes back here
            // through the controller's knockdown handler.
            if (_ragdoll.IsKnockdownTimed)
            {
                return;
            }

            if (intent == BodyPresentationState.Downed)
            {
                if (_ragdoll.Presentation != BodyPresentationState.Downed)
                {
                    _ragdoll.ServerSetPresentation(BodyPresentationState.Downed);
                }

                _healthOwnsFloor = true;
                return;
            }

            // Latch health-owned floors so we do not clear a combat knockdown health didn't start.
            if (!_healthOwnsFloor)
            {
                return;
            }

            _healthOwnsFloor = false;
            if (_ragdoll.Presentation is BodyPresentationState.Collapsed or BodyPresentationState.Downed)
            {
                _ragdoll.ServerSetPresentation(BodyPresentationState.Locomotion);
            }
        }

        /// <summary>
        /// Losing all arm control empties both hands; an arm newly crossing into Disabled empties its own hand
        /// (health.md §5).
        /// </summary>
        public static void DropHeldItems(Component body, bool lostArmControl, int newlyDisabledZones)
        {
            if ((!lostArmControl && newlyDisabledZones == 0) || !body.TryGetComponent(out Hands hands))
            {
                return;
            }

            foreach (Hand hand in hands.PlayerHands)
            {
                if (hand == null || hand.Container == null || hand.IsEmpty())
                {
                    continue;
                }

                BodyZone arm = hand.Side == HandSide.Left ? BodyZone.LeftArm : BodyZone.RightArm;
                if (!lostArmControl && (newlyDisabledZones & (1 << (int)arm)) == 0)
                {
                    continue;
                }

                hand.ItemInHand.GiveOwnership(null);
                hand.Container.Dump();
            }
        }
    }
}
