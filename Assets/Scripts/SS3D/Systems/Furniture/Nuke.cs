using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Inventory.Items.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class Nuke : InteractionSource, IInteractionTarget
    {
        [SerializeField] private Sprite _sprite;

        [ServerRpc(RequireOwnership = false)]
        public void Detonate()
        {
            // Ends the round, regardless of how many objectives were completed
            Subsystems.Get<GamemodeSystem>().EndRound();
        }

        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            _sprite = Icons.Get<Sprite>(InteractionIcons.Nuke);

            return new IInteraction[] { new NukeDetonateInteraction { Icon = _sprite } };
        }
    }
}