using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Items
{
    /// <summary>
    /// The syndies love to use this to blow up the place
    /// </summary>
    public class NukeCard : Item
    {        
        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.CreateTargetInteractions(interactionEvent).ToList();

            return interactions.ToArray();
        }
    }
}