using UnityEngine;

namespace SS3D.Engine.Interactions
{
    public interface IInteractionOriginProvider
    {
        Vector3 InteractionOrigin { get; }
    }
}