using FishNet.Object;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Furniture;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Interaction to allow sitting
    /// </summary>
    public sealed class SitInteraction : IInteraction
    {
        public SitInteraction(float timeToSit)
        {
            TimeToSit = timeToSit;
        }

        public float TimeToSit { get; private set; }

        public InteractionType InteractionType => InteractionType.Sit;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        public string GetName(InteractionEvent interactionEvent) => "Sit";

        public string GetGenericName() => "Sit";

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Discard;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!interactionEvent.Target.GetGameObject().TryGetComponent(out Sittable sit))
            {
                return false;
            }

            if (!GoodDistanceFromRootToSit(sit.transform, interactionEvent.Source.GameObject.transform))
            {
                return false;
            }

            return true;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = interactionEvent.Source.GetRootSource() as Hand;

            if (interactionEvent.Target is not Sittable sit || !hand)
            {
                return false;
            }

            hand.GetComponentInParent<ProceduralAnimationController>().PlayAnimation(InteractionType.Sit, hand, sit.NetworkObject, interactionEvent.Point, TimeToSit);
            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Debug.Log("attempting to cancel animation");

            if (interactionEvent.Source.GetRootSource() is Hand hand)
            {
                hand.GetComponentInParent<ProceduralAnimationController>().CancelAnimation(hand);
            }
        }

        private bool GoodDistanceFromRootToSit(Transform sit, Transform playerRoot) => Vector3.Distance(playerRoot.position, sit.position) < 2f;
    }
}
