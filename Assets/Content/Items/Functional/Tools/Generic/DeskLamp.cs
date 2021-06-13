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
        public Material materialOn;
        public Material materialOff;

        public void Toggle()
        {
            light.enabled = !light.enabled;

            this.prefab.GetComponent<MeshRenderer>().sharedMaterial = light.enabled ?
                materialOn : materialOff;

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
                this.prefab.GetComponent<MeshRenderer>().sharedMaterial = materialOn;
            }
            else
            {
                light.enabled = false;
                this.prefab.GetComponent<MeshRenderer>().sharedMaterial = materialOff;
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