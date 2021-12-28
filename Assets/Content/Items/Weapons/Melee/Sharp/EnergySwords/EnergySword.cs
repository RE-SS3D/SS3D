using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;
using UnityEngine;

// This handles the Esword item
// you can turn it on and slash people with it
namespace SS3D.Content.Items.Weapons.Melee.Sharp.EnergySwords
{
    public class EnergySword : Item, IToggleable
    {
        // Animation stuff
        [SerializeField]
        private Animator animator;
        private bool on;
        private static readonly int OnHash = Animator.StringToHash("On");

        // Sound stuff, TODO: change audio to audioSource
        [SerializeField]
        private AudioSource swordAudio;
        [SerializeField]
        private AudioClip soundOn;
        [SerializeField]
        private AudioClip soundOff;

        // temporary until we have asset data
        public Sprite turnOnIcon;

        [ClientRpc]
        private void RpcSetBlade(bool on)
        {
            swordAudio.clip = on ? soundOn : soundOff;
            swordAudio.Play();
            animator.SetBool(OnHash, on);
        }

        public bool GetState()
        {
            return on;
        }

        // TODO: rename this to SetBladeState
        public void Toggle()
        {
            if (swordAudio.isPlaying || animator.IsInTransition(0))
                return;

            on = !on;

            swordAudio.clip = on ? soundOn : soundOff;
            swordAudio.Play();

            animator.SetBool(OnHash, on);
            RpcSetBlade(on);
        }
    
        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            List<IInteraction> list = base.GenerateInteractionsFromTarget(interactionEvent).ToList();
            list.Add(new ToggleInteraction
            {
                OnName = "Turn off",
                OffName = "Turn on",
                IconOn = turnOnIcon,
                IconOff = turnOnIcon
            }); ;
            return list.ToArray();
        }
    }
}
