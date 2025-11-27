using FishNet.Connection;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Dragging
{
    public class DragInteraction : ContinuousInteraction
    {
        private Draggable _draggedObject;

        private NetworkConnection _previousOwner;

        public DragInteraction(float timeToReachGrabPlace)
        {
            TimeToReachGrabPlace = timeToReachGrabPlace;
            Delay = TimeToReachGrabPlace;
        }

        public override InteractionType InteractionType => InteractionType.Grab;

        public float TimeToReachGrabPlace { get; private set; }

        public override IClientInteraction CreateClient(InteractionEvent interactionEvent) => new ClientDelayedInteraction();

        public override string GetName(InteractionEvent interactionEvent) => "Grab";

        public override string GetGenericName() => "Grab";

        public override Sprite GetIcon(InteractionEvent interactionEvent) => throw new System.NotImplementedException();

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // Can only grab with hand
            if (interactionEvent.Source.GetRootSource() is not IItemHolder hand)
            {
                return false;
            }

            if (interactionEvent.Target is not Draggable grabbable)
            {
                return false;
            }

            // check that our hand is empty
            if (!hand.Empty)
            {
                return false;
            }

            return true;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source.GetRootSource() is IInteractionSourceAnimate animatedSource)
            {
                animatedSource.CancelSourceAnimation(InteractionType, interactionEvent.Target.GetComponent<NetworkObject>(), Delay);
            }

            // previous owner regain authority when not grabbed anymore
            if (_draggedObject != null)
            {
                _draggedObject.GiveOwnership(_previousOwner);
            }
        }

        protected override bool CanKeepInteracting(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return true;
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Draggable grabbable = interactionEvent.Target as Draggable;

            if (interactionEvent.Source.GetRootSource() is IInteractionSourceAnimate animatedSource)
            {
                animatedSource.PlaySourceAnimation(InteractionType, interactionEvent.Target.GetComponent<NetworkObject>(), grabbable.GameObject.transform.position, Delay);
            }

            return true;
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            _draggedObject = interactionEvent.Target as Draggable;
            _previousOwner = _draggedObject.Owner;
            _draggedObject.NetworkObject.GiveOwnership(interactionEvent.Source.GetRootSource().NetworkObject.Owner);
        }
    }
}
