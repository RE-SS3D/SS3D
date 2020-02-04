using UnityEngine;
using System.Collections;
using Mirror;

namespace Interactions2.Custom
{
    public class Flashlight : MonoBehaviour, Core.ContinuousInteraction
    {
        [SerializeField]
        private Light light;

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
            RpcSetLight(true);
        }

        public bool ContinueInteracting(GameObject tool, GameObject target, RaycastHit hit)
        {
            return true;
        }

        public void EndInteraction()
        {
            RpcSetLight(false);
        }

        private void RpcSetLight(bool value)
        {
            light.enabled = value;
        }
    }
}
