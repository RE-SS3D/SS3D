using UnityEngine;
using System.Collections;
using Mirror;

namespace Interactions2.Custom
{
    public class Flashlight : NetworkBehaviour, Core.ContinuousInteraction
    {
        [SerializeField]
        private new Light light = null;

        public Core.InteractionEvent Event { get; set; }
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
