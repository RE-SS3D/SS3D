using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Items;
using SS3D.Systems.GameModes.GameEvents;
using FishNet.Object;
using UnityEngine;
using System.Collections;

namespace SS3D.Systems.Furniture
{
    public class Nuke : InteractionSourceNetworkBehaviour, IInteractionTarget
    {
        [SerializeField] private Sprite _sprite;
        [SerializeField] private GameEvent detonateEvent;

        [ServerRpc(RequireOwnership = false)]
        public void Detonate()
        {
            Debug.Log("Boom!");
            Hashtable gameEventData = new Hashtable();
            detonateEvent.Raise(gameEventData);

            RpcDetonate();
        }

        [ObserversRpc]
        private void RpcDetonate()
        {
            if (IsServer) { return; }

            Debug.Log("Boom!");
            detonateEvent.Raise(new Hashtable());
        }

        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new NukeDetonateInteraction { Icon = _sprite } };
        }
    }
}