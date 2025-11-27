using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Represents a target that can be interacted with
    /// </summary>
    public interface IInteractionTarget : IGameObjectProvider
    {
        /// <summary>
        /// Creates possible interactions (not checked for CanExecute)
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <returns>All created interactions</returns>
        IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent);

        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point);

    }
}
