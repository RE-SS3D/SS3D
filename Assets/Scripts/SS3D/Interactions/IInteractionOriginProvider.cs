using UnityEngine;

namespace SS3D.Interactions
{
    public interface IInteractionOriginProvider
    {
        Vector3 InteractionOrigin { get; }
    }
}