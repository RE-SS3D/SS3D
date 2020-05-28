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
        
        public override IInteraction[] GenerateInteractions(IInteractionTarget[] targets)
        {
            List<IInteraction> generateInteractions = base.GenerateInteractions(targets).ToList();
            var flashlightInteraction = new FlashlightInteraction();
            flashlightInteraction.icon = turnOnIcon;
            generateInteractions.Insert(0, flashlightInteraction);
            return generateInteractions.ToArray();
        }
    }
}