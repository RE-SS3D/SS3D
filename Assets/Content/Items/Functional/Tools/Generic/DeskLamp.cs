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
    public class DeskLamp : Item, IToggleable
    {
        [SerializeField]
        public new Light light = null;
        public Sprite toggleIcon;

        public void Toggle()
        {
            light.enabled = !light.enabled;
            RpcToggle(light.enabled);
        }

        public bool GetState()
        {
            return light.enabled;
        }

        [ClientRpc]
        private void RpcToggle(bool lightEnabled) 
        {
            if (lightEnabled)
            {
                light.enabled = true; 
            }
            else
            {
                light.enabled = false;
            }
        }

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.GenerateInteractionsFromTarget(interactionEvent).ToList();
            interactions.Add(new ToggleInteraction{ IconOn = toggleIcon, IconOff = toggleIcon });
            return interactions.ToArray();
        }
    }
}