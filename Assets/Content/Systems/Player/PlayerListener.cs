using System;
using Mirror;
using UnityEngine;

namespace SS3D.Content.Systems.Player
{
    [RequireComponent(typeof(AudioListener))]
    public class PlayerListener : MonoBehaviour
    {
        private AudioListener listener;

        private void Start()
        {
            if (NetworkClient.active)
            {
                if (NetworkClient.connection.identity.gameObject != transform.parent.gameObject)
                {
                    // Destroy if listener of other player
                    Destroy(gameObject);
                }
            }
            
            else if (NetworkServer.active)
            {
                // Destroy if server only
                Destroy(gameObject);
            }
        }
    }
}
