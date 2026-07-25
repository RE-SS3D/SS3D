using SS3D.Data;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;
using SS3D.Data.Generated;

namespace SS3D.Systems.Combat.Interactions
{
    /// <summary>
    /// Interaction to hit another player.
    /// </summary>
    public class HitInteraction : IInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Hit";
        }

        public string GetGenericName() => throw new System.NotImplementedException();

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Nuke);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            // Curently just hit the first body part of an entity if it finds one.
            // Should instead choose the body part using the target dummy doll ?
            // Also should be able to hit with other things than just hands.
            if (target is IGameObjectProvider targetBehaviour && source is Hand hand)
            {
                Entity entity = targetBehaviour.GameObject.GetComponentInParent<Entity>();

                if (entity == null)
                    return false;

                BodyPart bodyPart = entity.GetComponentInChildren<BodyPart>();

                if (bodyPart == null)
                    return false;

                bool isInRange = InteractionExtensions.RangeCheck(interactionEvent);

                return isInRange;
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            // Curently just hit the first body part of an entity if it finds one.
            // Should instead choose the body part using the target dummy doll ?
            // Also should be able to hit with other things than just hands.
            if (target is IGameObjectProvider targetBehaviour && source is Hand hand)
            {
                Entity entity = targetBehaviour.GameObject.GetComponentInParent<Entity>();
                BodyPart bodyPart = entity.GetComponentInChildren<BodyPart>();

                // Inflict a fix amount and type of damages for now. Long term, should be passed in parameter and depends on weapon type, velocity ...
                bodyPart.InflictDamageToAllLayer(new DamageTypeQuantity(DamageType.Slash, 50));

                TriggerAttackerHitAnimation(hand);
            }

            return false;
        }

        /// <summary>
        /// Plays the attacker's own hit-swing animation. Runs server-side (this method is only
        /// reached from Start, which executes authoritatively on the server), so it goes through
        /// ServerTriggerHit rather than setting the Animator trigger directly.
        /// </summary>
        private static void TriggerAttackerHitAnimation(Hand hand)
        {
            Hands attackerHands = hand.HandsController;

            if (attackerHands == null)
            {
                return;
            }

            HumanoidAnimatorController animatorController = attackerHands.GetComponent<HumanoidAnimatorController>();

            if (animatorController == null)
            {
                return;
            }

            HandSide side = attackerHands.PlayerHands.IndexOf(hand) == 0 ? HandSide.Left : HandSide.Right;
            animatorController.ServerTriggerHit(side);
        }
    }
}