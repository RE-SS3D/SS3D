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
        /// True when <see cref="Point"/> and <see cref="Normal"/> come from a resolved hit.
        /// False when no point was resolved — do not treat <see cref="Point"/> as a world location.
        /// </summary>
        public bool HasPoint { get; private set; }
        /// <summary>
        /// The point at which the interaction took place. Meaningful only when <see cref="HasPoint"/> is true
        /// (including a real hit at world origin).
        /// </summary>
        public Vector3 Point { get; private set; }
        /// <summary>
        /// The normal angle at which the interacted surface is facing. Meaningful only when <see cref="HasPoint"/> is true.
        /// </summary>
        public Vector3 Normal { get; private set; }

        /// <summary>
        /// Creates an event with no resolved interaction point.
        /// </summary>
        public InteractionEvent(IInteractionSource source, IInteractionTarget target)
        {
            Source = source;
            Target = target;
            HasPoint = false;
            Point = default;
            Normal = default;
        }

        /// <summary>
        /// Creates an event with a resolved interaction point (including world origin).
        /// </summary>
        public InteractionEvent(IInteractionSource source, IInteractionTarget target, Vector3 point, Vector3 normal)
        {
            Source = source;
            Target = target;
            HasPoint = true;
            Point = point;
            Normal = normal;
        }

        /// <summary>
        /// Hot-path reuse: update hit data without allocating a new event (outline LateUpdate).
        /// </summary>
        public void SetResolvedHit(Vector3 point, Vector3 normal)
        {
            HasPoint = true;
            Point = point;
            Normal = normal;
        }

        /// <summary>
        /// Hot-path reuse: clear hit data without allocating a new event (outline LateUpdate).
        /// </summary>
        public void ClearResolvedHit()
        {
            HasPoint = false;
            Point = default;
            Normal = default;
        }

        /// <summary>
        /// Copies target/point/normal onto a different source (armed-mode combine).
        /// </summary>
        public InteractionEvent WithSource(IInteractionSource source)
        {
            return HasPoint
                ? new InteractionEvent(source, Target, Point, Normal)
                : new InteractionEvent(source, Target);
        }

        /// <summary>
        /// Copies source/point/normal onto a different target (Discover per-target checks).
        /// </summary>
        public InteractionEvent WithTarget(IInteractionTarget target)
        {
            return HasPoint
                ? new InteractionEvent(Source, target, Point, Normal)
                : new InteractionEvent(Source, target);
        }
    }
}
