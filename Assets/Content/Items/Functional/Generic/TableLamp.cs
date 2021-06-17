using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Items.Functional.Tools
{
    // Simple table lamp
    public class TableLamp : Item, IToggleable
    {
        [SerializeField]
        public new Light light = null;
        public Sprite toggleIcon;
        public Material materialOn;
        public Material materialOff;

        private MeshRenderer meshRenderer;

        public void Start()
        {
            meshRenderer = this.prefab.GetComponent<MeshRenderer>();
            meshRenderer.material = light.enabled ? materialOn : materialOff;
        }

        public void Toggle()
        {
            light.enabled = !light.enabled;
            meshRenderer.material = light.enabled ? materialOn : materialOff;

            RpcToggle(light.enabled);
        }

        public bool GetState()
        {
            return light.enabled;
        }

        [ClientRpc]
        private void RpcToggle(bool lightEnabled) 
        {
            light.enabled = lightEnabled;
            meshRenderer.material = light.enabled ? materialOn : materialOff;
        }

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.GenerateInteractionsFromTarget(interactionEvent).ToList();
            interactions.Add(new ToggleInteraction{ IconOn = toggleIcon, IconOff = toggleIcon });
            return interactions.ToArray();
        }
    }
}