using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    public class InteractionEvent
    {
        public InteractionEvent(IInteractionSource source, IInteractionTarget target, Vector3 point = default)
        {
            Source = source;
            Target = target;
            Point = point;
        }

        /// <summary>
        /// The source which caused the interaction
        /// </summary>
        public IInteractionSource Source { get; }

        /// <summary>
        /// The target of the interaction, can be null
        /// </summary>
        public IInteractionTarget Target { get; set; }

        /// <summary>
        /// The point at which the interaction took place
        /// </summary>
        public Vector3 Point { get; }
    }
}