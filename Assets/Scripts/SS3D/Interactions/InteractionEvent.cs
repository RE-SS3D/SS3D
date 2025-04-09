using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    public class InteractionEvent
    {
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
        /// <summary>
        /// The normal angle at which the interacted surface is facing
        /// </summary>
        public Vector3 Normal { get; }

        public InteractionEvent(IInteractionSource source, IInteractionTarget target, Vector3 point = new(), Vector3 normal = new())
        {
            Source = source;
            Target = target;
            Point = point;
            Normal = normal;
        }
    }
}