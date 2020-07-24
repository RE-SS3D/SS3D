using UnityEngine;
using Mirror;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Items.Functional.Generic
{
    public class Boombox : NetworkBehaviour, Interaction
    {
        [SerializeField]
        private new Light light = null;
        private GameObject bulb = null;
        private Material onmat = null;
        private Material offmat = null;

        public InteractionEvent Event { get; set; }
        public string Name => "Turn On/Off";

        public void OnEnable()
        {
            light.enabled = false;
        }

        public bool CanInteract()
        {
            return Event.tool == gameObject;
        }

        public void Interact()
        {
            light.enabled = !light.enabled;
            RpcSetLight(light.enabled);
          
        }

        [ClientRpc]
        private void RpcSetLight(bool value)
        {
            light.enabled = value;
            Material mat = null;
            if (value)
            {
                mat = onmat;
            }
            else
            {
                mat = offmat;
            }
            bulb.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}