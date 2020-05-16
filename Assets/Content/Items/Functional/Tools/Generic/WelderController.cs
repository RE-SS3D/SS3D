using UnityEngine;
using Mirror;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Items.Functional.Tools
{
    public class WelderController : NetworkBehaviour, Interaction
    {
        [SerializeField]
        public ParticleSystem pSystem;

        public InteractionEvent Event { get; set; }
        public string Name => "Turn On/Off";

        public void OnEnable()
        {
            pSystem.enableEmission = false;
        }

        public bool CanInteract()
        {
            return Event.tool == gameObject;
        }

        public void Interact()
        {
            pSystem.enableEmission = !pSystem.enableEmission;
            if (pSystem.isEmitting)
            {
                pSystem.Stop();
            } else
            {
                pSystem.Play();
            }
            RpcTurnOn(!pSystem.isEmitting);
        }

        [ClientRpc]
        private void RpcTurnOn(bool value)
        {
            if (value)
            {
                pSystem.Stop();
            }
            else
            {
                pSystem.Play();
            }
        }
    }
}