using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    [Serializable]
    public class OpenInteraction : DelayedInteraction
    {
        private IOpenable _openable;

        public OpenInteraction(IOpenable openable)
        {
            _openable = openable;
        }

        public override InteractionType InteractionType => InteractionType.Open;

        [NotNull]
        public override string GetGenericName() => "Open";

        [NotNull]
        public override string GetName(InteractionEvent interactionEvent)
        {
            if (_openable == null)
            {
                return string.Empty;
            }

            string name = interactionEvent.Target.GameObject.name;
            return _openable.IsOpen ? "Close " + name : "Open " + name;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Open;

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // Check whether the object is in range
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            return interactionEvent.Target is OpenableContainer;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Debug.Log("in OpenInteraction, Start");
            _openable.SetOpen(!_openable.IsOpen);
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IItemHolder hand = interactionEvent.Source as IItemHolder;

            Vector3 point = interactionEvent.Point;

            if (interactionEvent.Target.TryGetInteractionPoint(interactionEvent.Source, out Vector3 customPoint))
            {
                point = customPoint;
            }

            if (hand != null && hand is IInteractionSourceAnimate animatedSource)
            {
                animatedSource.PlaySourceAnimation(InteractionType, interactionEvent.Target.GetComponent<NetworkObject>(), point, Delay);
            }

            return true;
        }
    }
}
