using UnityEngine;
using SS3D.Content.Furniture;
using Mirror;

namespace SS3D.Content.Items.Functional.Generic
{
    // believe it or not this handles opening and lighting a lighter
    public class LighterOpenable : NetworkedOpenable
    {
	// the particle system for the flames
        [SerializeField]
        private ParticleSystem fireParticle;

	// TODO: Fuel
        protected override void OnOpenStateChange(object sender, bool e)
        {
            base.OnOpenStateChange(sender, e);
            SetFlame(e);
        }

        private void SetFlame(bool on)
        {
            if (on)
            {
                fireParticle.Play();
            }
            else
            {
                fireParticle.Stop();
            }
            RpcSetFlame(on);
        }

        [ClientRpc]
        private void RpcSetFlame(bool on)
        {
            if (on)
            {
                fireParticle.Play();
            }
            else
            {
                fireParticle.Stop();
            }
        }
    }
}

