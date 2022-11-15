using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Items;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Systems.GameModes;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class Nuke : InteractionSourceNetworkBehaviour, IInteractionTarget
    {
        [SerializeField] private Sprite _sprite;

        [ServerRpc(RequireOwnership = false)]
        public void Detonate()
        {
            if (!IsServer) { return; }

            Debug.Log("Boom!");

            RpcDetonate();
        }

        [ObserversRpc]
        private void RpcDetonate()
        {
            if (IsServer) { return; }

            Debug.Log("Boom!");
        }

        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new NukeDetonateInteraction { Icon = Database.Icons.Get(InteractionIcons.Nuke) } };
        }
    }
}