using UnityEngine;
using Mirror;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Items.Functional.Tools
{
    public class Flashlight : NetworkBehaviour, ContinuousInteraction
    {
        [SerializeField]
        private new Light light = null;

        public InteractionEvent Event { get; set; }
        public string Name => "Turn on";

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
            light.enabled = true;
            RpcSetLight(true);
        }
        public bool ContinueInteracting() => true;
        public void EndInteraction()
        {
            light.enabled = false;
            RpcSetLight(false);
        }

        [ClientRpc]
        private void RpcSetLight(bool value)
        {
            light.enabled = value;
        }
    }
}
