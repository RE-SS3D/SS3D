using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    public interface IInteractionPointProvider
    {
        /// <summary>
        /// Given a source interaction, tries to return a point where the interaction should occur. 
        /// </summary>
        public bool TryGetInteractionPoint(IInteractionSource source, out Vector3 point);
    }
}
