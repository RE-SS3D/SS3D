using UnityEngine;
using System.Collections;
using Mirror;

namespace Interactions2.Custom
{
    public class Flashlight : NetworkBehaviour, Core.ContinuousInteraction
    {
        [SerializeField]
        private new Light light = null;

        public NetworkConnection ConnectionToClient { get; set; }

        public void OnEnable()
        {
            light.enabled = false;
        }

        public bool CanInteract(GameObject tool, GameObject target, RaycastHit at)
        {
            return tool == gameObject;
        }

        public void Interact(GameObject tool, GameObject target, RaycastHit at)
        {
            light.enabled = true;
            RpcSetLight(true);
        }

        public bool ContinueInteracting(GameObject tool, GameObject target, RaycastHit hit)
        {
            return true;
        }

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
