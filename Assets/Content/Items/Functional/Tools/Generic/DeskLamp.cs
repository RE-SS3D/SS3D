using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Items.Functional.Tools
{
    // Simple desklamp
    public class DeskLamp : Item
    {
        [SerializeField]
        public new Light light = null;
        public Sprite turnOnIcon;
        
        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            List<IInteraction> list = base.GenerateInteractionsFromTarget(interactionEvent).ToList();
            list.Add(new DeskLampInteraction{ icon = turnOnIcon });
            return list.ToArray();
        }
    }
}