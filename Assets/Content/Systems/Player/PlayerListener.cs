using System;
using Mirror;
using UnityEngine;

namespace SS3D.Content.Systems.Player
{
    [RequireComponent(typeof(AudioListener))]
    public class PlayerListener : NetworkBehaviour
    {
        private AudioListener listener;

        private void Start()
        {
            if (NetworkClient.active)
            {
                if (!GetComponentInParent<NetworkIdentity>().isLocalPlayer)
                {
                    // Destroy if listener of other player
                    Destroy(gameObject);
                }
            }
        }
    }
}
