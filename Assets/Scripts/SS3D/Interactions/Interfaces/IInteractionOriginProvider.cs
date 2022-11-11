using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    public interface IInteractionOriginProvider
    {
        Vector3 InteractionOrigin { get; }
    }
}