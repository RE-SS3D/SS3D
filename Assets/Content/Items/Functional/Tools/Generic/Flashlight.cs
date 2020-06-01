using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Items.Functional.Tools
{
    public class Flashlight : Item
    {
        [SerializeField]
        public new Light light = null;
        public Sprite turnOnIcon;
        
        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> list = base.GenerateInteractions(interactionEvent).ToList();
            list.Add(new FlashlightInteraction{ icon = turnOnIcon });
            return list.ToArray();
        }
    }
}