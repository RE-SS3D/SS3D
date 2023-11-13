using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Script controlling the opening and closing of airlocks.
    /// </summary>
    public class AirLockOpener : NetworkBehaviour
    {
        private const float DOOR_WAIT_CLOSE_TIME = 2.0f;

        [SerializeField]
        private Animator _animator;

        // This defines the animation we want to trigger
        private static readonly int OpenId = Animator.StringToHash("Open");

        [SerializeField] private LayerMask doorTriggerLayers = -1;


        private int playersInTrigger; // Server Only
        private Coroutine closeTimer; // Server Only


        public void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;
            if ((1 << other.gameObject.layer & doorTriggerLayers) == 0) return;

            if (playersInTrigger == 0)
            {
                if (closeTimer != null)
                {
                    StopCoroutine(closeTimer);
                    closeTimer = null;
                }
                SetOpen(true);
            }

            playersInTrigger += 1;
        }

        public void OnTriggerExit(Collider other)
        {
            if(!IsServer) return;
            if ((1 << other.gameObject.layer & doorTriggerLayers) == 0) return;

            if (playersInTrigger == 1)
            {
                // Start the close timer (which may be stopped).
                closeTimer = StartCoroutine(RunCloseEventually(DOOR_WAIT_CLOSE_TIME));
            }

            playersInTrigger = Math.Max(playersInTrigger - 1, 0);
        }

        private IEnumerator RunCloseEventually(float time)
        {
            yield return new WaitForSeconds(time);
            SetOpen(false);
        }

        [Server]
        private void SetOpen(bool open)
        {
            _animator.SetBool(OpenId, open);
        }
    }
}
