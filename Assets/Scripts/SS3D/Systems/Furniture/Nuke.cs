using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Items;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Systems.Gamemodes;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class Nuke : InteractionSourceNetworkBehaviour, IInteractionTarget
    {
        [SerializeField] private Sprite _sprite;

        [ServerRpc(RequireOwnership = false)]
        public void Detonate()
        {
            // Ends the round, regardless of how many objectives were completed
            SystemLocator.Get<GamemodeSystem>().EndRound();
        }

        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new NukeDetonateInteraction { Icon = Database.Icons.Get(InteractionIcons.Nuke) } };
        }
    }
}