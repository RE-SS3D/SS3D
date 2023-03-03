using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Represents the origin position of an interaction
    /// </summary>
    public interface IInteractionOriginProvider
    {
        Vector3 InteractionOrigin { get; }
    }
}