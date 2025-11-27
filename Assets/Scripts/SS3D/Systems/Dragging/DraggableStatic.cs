using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Dragging
{
    /// <summary>
    /// For furnitures that can be dragged
    /// </summary>
    public sealed class DraggableStatic : Draggable
    {
        public override bool MoveToGrabber => false;

        public override bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point) => this.GetInteractionPoint(source, out point);
    }
}
