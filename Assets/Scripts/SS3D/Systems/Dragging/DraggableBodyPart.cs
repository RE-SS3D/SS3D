using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Dragging;
using UnityEngine;

namespace SS3D.Systems.Animations
{
    /// <summary>
    /// Put that on body parts that can be grabbed
    /// </summary>
    public class DraggableBodyPart : Draggable
    {
        public override bool MoveToGrabber => true;

        public override bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point)
        {
            point = gameObject.transform.position;

            if (gameObject.TryGetComponent(out Rigidbody rigibody))
            {
                point = rigibody.centerOfMass;
            }

            return true;
        }
    }
}
