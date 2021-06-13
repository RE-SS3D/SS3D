using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Items.Functional.Tools
{
    // Simple flashlight
    public class Flashlight : Item, IToggleable
    {
        [SerializeField]
        public new Light light = null;
        public Sprite toggleIcon;
        public Material bulbMaterialOn;
        public Material bulbMaterialOff;

        public GameObject bulbObject;
        
        public void Toggle()
        {
            light.enabled = !light.enabled;
            
            bulbObject.GetComponent<MeshRenderer>().sharedMaterial = (light.enabled ? 
                bulbMaterialOn : bulbMaterialOff);

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
                bulbObject.GetComponent<MeshRenderer>().sharedMaterial = bulbMaterialOn;
            }
            else
            {
                light.enabled = false;
                bulbObject.GetComponent<MeshRenderer>().sharedMaterial = bulbMaterialOff;
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