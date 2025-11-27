using FishNet.Object;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Animations;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Interface for tools to play an interaction
    /// </summary>
    public interface IInteractiveTool : IPlayInteractionAnimation, IGameObjectProvider, INetworkObjectProvider
    {
        /// <summary>
        /// Point that should align with the interaction target position. For instance, the tip of a screwdriver.
        /// </summary>
        public Transform InteractionPoint { get; }

        public NetworkBehaviour NetworkBehaviour { get; }
    }
}
