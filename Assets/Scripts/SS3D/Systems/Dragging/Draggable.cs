using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Dragging
{
    /// <summary>
    /// Put this script on things that can be dragged by a player, such as unbolted furnitures.
    /// </summary>
    public abstract class Draggable : NetworkActor, IInteractionTarget
    {
        /// <summary>
        /// True if the object is currently being dragged.
        /// </summary>
        [SyncVar]
        private bool _dragged;

        /// <summary>
        /// If true, when grabbing, the draggable root transform should be moved toward the grabber, otherwise it's the grabber that moves toward the grabbed item.
        /// For heavy, rigid stuff this should be false, for stuff such as ragdoll this should be true.
        /// </summary>
        public abstract bool MoveToGrabber { get; }

        public bool Dragged => _dragged;

        [NotNull]
        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new DragInteraction(1f),
            };
        }

        public abstract bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point);
    }
}
