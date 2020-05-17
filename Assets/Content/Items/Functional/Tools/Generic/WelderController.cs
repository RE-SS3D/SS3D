using UnityEngine;
using Mirror;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Items.Functional.Tools
{
    public class WelderController : NetworkBehaviour, Interaction
    {
        [SerializeField]
        public ParticleSystem hotFlame;
        [SerializeField]
        public ParticleSystem coldFlame;
        [SerializeField]
        public ParticleSystem lightParticle;

        public InteractionEvent Event { get; set; }
        public string Name => "Turn On/Off";

        public void OnEnable()
        {
            hotFlame.enableEmission = false;
        }

        public bool CanInteract()
        {
            return Event.tool == gameObject;
        }

        public void Interact()
        {
            hotFlame.enableEmission = !hotFlame.enableEmission;
            if (hotFlame.isEmitting)
            {
                hotFlame.Stop();
                coldFlame.Stop();
                lightParticle.Stop();
            } else
            {
                hotFlame.Play();
                coldFlame.Play();
                lightParticle.Play();
            }
            RpcTurnOn(!hotFlame.isEmitting);
        }

        [ClientRpc]
        private void RpcTurnOn(bool value)
        {
            if (value)
            {
                hotFlame.Stop();
                coldFlame.Stop();
                lightParticle.Stop();
            }
            else
            {
                hotFlame.Play();
                coldFlame.Play();
                lightParticle.Play();
            }
        }
    }
}