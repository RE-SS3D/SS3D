using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Rounds;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class Nuke : InteractionSource, IInteractionTarget
    {
        /// <summary>
        /// Server entry. Deliberately not a <c>ServerRpc</c>: the detonate interaction's
        /// <c>Start</c> already runs on the server, and FishNet drops a ServerRpc invoked from
        /// server code on a dedicated server (the generated writer returns when <c>!IsClient</c>),
        /// so detonation silently did nothing there. Only the server ever calls this.
        /// </summary>
        [Server]
        public void Detonate()
        {
            // Ends the round, regardless of how many objectives were completed
            SubSystems.Get<GamemodeSubSystem>().EndRound(RoundEndRequest.Detonation);
        }

        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new NukeDetonateInteraction { Icon = Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Nuke) } };
        }
    }
}