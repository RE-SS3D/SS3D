using UnityEngine;
using SS3D.Content.Furniture;
using Mirror;

namespace SS3D.Content.Items.Functional.Generic.Lighters
{
    public class LighterOpenable : NetworkedOpenable
    {
        [SerializeField]
        private ParticleSystem fireParticle;

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

