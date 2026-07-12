using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Electricity;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Script controlling the opening and closing of airlocks.
    /// When a player get close to an airlock, open the airlock. Keep the airlock opened as long as a player
    /// is close to the opened airlock. When no player are close to it, close the airlock.
    /// </summary>
    public class AirLockOpener : NetworkBehaviour
    {
        /// <summary>
        /// Time in second before the door start to close when player are out of the trigger collider of the airlock.
        /// </summary>
        private const float DOOR_WAIT_CLOSE_TIME = 2.0f;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        /// <summary>
        /// The animation's id of the animation we want to trigger
        /// </summary>
        private static readonly int OpenId = Animator.StringToHash("Open");

        [SerializeField] private LayerMask doorTriggerLayers = -1;

        /// <summary>
        /// Number of player close enough to the airlock.
        /// </summary>
        private int playersInTrigger; // Server Only

        /// <summary>
        /// Coroutine to eventually close the door when no one is around.
        /// </summary>
        private Coroutine closeTimer; // Server Only


        [SerializeField]
        private List<MeshRenderer> _meshesToColor;

        public ReadOnlyCollection<MeshRenderer> MeshesToColor => _meshesToColor.AsReadOnly();


        [SerializeField]
        private List<SkinnedMeshRenderer> _skinnedMeshesToColor;

        public ReadOnlyCollection<SkinnedMeshRenderer> SkinnedMeshesToColor => _skinnedMeshesToColor.AsReadOnly();

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_powerConsumer == null)
            {
                TryGetComponent(out _powerConsumer);
            }

            if (_powerConsumer != null)
            {
                _powerConsumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
            }
        }

        public override void OnStopServer()
        {
            if (_powerConsumer != null)
            {
                _powerConsumer.OnPowerStatusUpdated -= HandlePowerStatusUpdated;
            }

            base.OnStopServer();
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;
            if ((1 << other.gameObject.layer & doorTriggerLayers) == 0) return;
            if (!IsPowered()) return;

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

            playersInTrigger = Math.Max(playersInTrigger - 1, 0);

            if (playersInTrigger == 0)
            {
                ScheduleCloseAfterDelay();
            }
        }

        private IEnumerator RunCloseEventually(float time)
        {
            yield return new WaitForSeconds(time);
            SetOpen(false);
        }

        [Server]
        private void SetOpen(bool open)
        {
            if (open && !IsPowered())
            {
                return;
            }

            _animator.SetBool(OpenId, open);
        }

        private void HandlePowerStatusUpdated(object sender, PowerStatus newStatus)
        {
            if (!IsServer || newStatus == PowerStatus.Powered)
            {
                return;
            }

            ScheduleCloseAfterDelay();
        }

        private void ScheduleCloseAfterDelay()
        {
            if (closeTimer != null)
            {
                StopCoroutine(closeTimer);
                closeTimer = null;
            }

            // Keep the door open while someone is still in the trigger; normal exit timing applies.
            if (playersInTrigger > 0)
            {
                return;
            }

            closeTimer = StartCoroutine(RunCloseEventually(DOOR_WAIT_CLOSE_TIME));
        }

        private bool IsPowered()
        {
            return _powerConsumer == null || _powerConsumer.PowerStatus == PowerStatus.Powered;
        }
    }
}
