using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Intents;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SS3D.Systems.Animations
{
    public class ProceduralAnimationController : NetworkActor
    {
        // We can't have more than one procedural animation running at the same time per hand (maybe should store Source instead of hand).
        private readonly List<Tuple<Hand, IProceduralAnimation>> _animations = new();

        [field: SerializeField]
        public TargetFollow LookAtTargetLocker { get; private set; }

        [field: SerializeField]
        public HoldController HoldController { get; private set; }

        [field: SerializeField]
        public MultiAimConstraint LookAtConstraint { get; private set; }

        [field: SerializeField]
        public PositionController PositionController { get; private set; }

        [field: SerializeField]
        public HumanoidAnimatorController AnimatorController { get; private set; }

        [field: SerializeField]
        public HumanoidMovementController MovementController { get; private set; }

        [field: SerializeField]
        public IntentController IntentController { get; private set; }

        [field: SerializeField]
        public Hands Hands { get; private set; }

        [Server]
        public void PlayAnimation(InteractionType interactionType, Hand hand,  NetworkObject target, Vector3 targetPosition, float time, float delay = 0f)
        {
            Hands.TryGetOppositeHand(hand, out Hand oppositeHand);
            ObserversPlayAnimation(interactionType, hand, oppositeHand, target, targetPosition, time, delay);
        }

        [Server]
        public void CancelAnimation(Hand hand)
        {
            ObserverCancelAnimation(hand);
        }

        [ObserversRpc]
        private void ObserversPlayAnimation(InteractionType interactionType, Hand mainHand, Hand secondaryHand, NetworkObject target, Vector3 targetPosition, float time, float delay = 0f)
        {
            IProceduralAnimation proceduralAnimation;
            switch (interactionType)
            {
                case InteractionType.Pickup:
                {
                    proceduralAnimation = new PickUpAnimation(this, time, mainHand, secondaryHand, target.GetComponent<AbstractHoldable>());
                    break;
                }

                case InteractionType.Place:
                {
                    proceduralAnimation = new PlaceAnimation(this, time, mainHand, secondaryHand, target.GetComponent<AbstractHoldable>(), targetPosition);
                    break;
                }

                case InteractionType.Screw:
                {
                    proceduralAnimation = new InteractAnimations(this, time, mainHand, target, interactionType, targetPosition);
                    break;
                }

                case InteractionType.Press:
                {
                    proceduralAnimation = new InteractWithHandAnimation(this, time, mainHand, targetPosition, interactionType);
                    break;
                }

                case InteractionType.Grab:
                {
                    proceduralAnimation = new DragAnimation(this, time, mainHand, secondaryHand, target);
                    break;
                }

                case InteractionType.Sit:
                {
                    proceduralAnimation = new SitAnimation(time, this, target);
                    break;
                }

                case InteractionType.Throw:
                {
                    proceduralAnimation = new ThrowAnimation(time, this, target, mainHand, secondaryHand);
                    break;
                }

                case InteractionType.Hit:
                {
                    proceduralAnimation = new HitAnimation(this, time, targetPosition, mainHand);
                    break;
                }

                default:
                    return;
            }

            _animations.Add(new(mainHand, proceduralAnimation));
            proceduralAnimation.ClientPlay();
            proceduralAnimation.OnCompletion += RemoveAnimation;
        }

        [ObserversRpc]
        private void ObserverCancelAnimation(Hand hand)
        {
            _animations.Find(x => x.Item1 == hand)?.Item2.Cancel();
            _animations.Remove(_animations.Find(x => x.Item1 == hand));
        }

        private void RemoveAnimation(IProceduralAnimation proceduralAnimation)
        {
            _animations.Remove(_animations.Find(x => x.Item2 == proceduralAnimation));
        }
    }
}
