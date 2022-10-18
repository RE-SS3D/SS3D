using UnityEngine;

namespace SS3D.Engine.Interactions
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

        public InteractionEvent(IInteractionSource source, IInteractionTarget target, Vector3 point = new Vector3())
        {
            Source = source;
            Target = target;
            Point = point;
        }
    }
}