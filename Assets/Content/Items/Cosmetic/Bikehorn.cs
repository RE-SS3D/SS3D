using UnityEngine;
using Mirror;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Items.Functional.Generic
{
    [RequireComponent(typeof(AudioSource))]
    public class Bikehorn : NetworkBehaviour, Interaction
    {
        [SerializeField] private AudioClip honkSound = null;
        private AudioSource audioSource;

        public InteractionEvent Event { get; set; }
        public string Name => "Honk";

        public bool CanInteract()
        {
            return Event.tool == gameObject;
        }

        public void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void Interact()
        {
            if (!audioSource.isPlaying) {
                audioSource.PlayOneShot(honkSound);
                RpcPlayHonk();
            }
        }

        [ClientRpc]
        private void RpcPlayHonk()
        {
            audioSource.PlayOneShot(honkSound);
        }
    }
}