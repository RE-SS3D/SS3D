using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Atmospherics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Serialization;

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
        private const float DoorWaitCloseTime = 2.0f;

        /// <summary>
        /// The animation's id of the animation we want to trigger
        /// </summary>
        private static readonly int OpenId = Animator.StringToHash("Open");

        [SerializeField]
        private Animator _animator;

        [FormerlySerializedAs("doorTriggerLayers")]
        [SerializeField]
        private LayerMask _doorTriggerLayers = -1;

        /// <summary>
        /// Number of player close enough to the airlock.
        /// </summary>
        private int _playersInTrigger; // Server Only

        /// <summary>
        /// Coroutine to eventually close the door when no one is around.
        /// </summary>
        private Coroutine _closeTimer; // Server Only

        [SerializeField]
        private List<SkinnedMeshRenderer> _skinnedMeshesToColor;

        [SerializeField]
        private List<MeshRenderer> _meshesToColor;

        public ReadOnlyCollection<MeshRenderer> MeshesToColor => _meshesToColor.AsReadOnly();

        public ReadOnlyCollection<SkinnedMeshRenderer> SkinnedMeshesToColor => _skinnedMeshesToColor.AsReadOnly();

        protected void OnTriggerEnter(Collider other)
        {
            if (!IsServer || (1 << other.gameObject.layer & _doorTriggerLayers) == 0)
            {
                return;
            }

            if (_playersInTrigger == 0)
            {
                if (_closeTimer != null)
                {
                    StopCoroutine(_closeTimer);
                    _closeTimer = null;
                }

                SetOpen(true);
            }

            _playersInTrigger += 1;
        }

        protected void OnTriggerExit(Collider other)
        {
            if (!IsServer || (1 << other.gameObject.layer & _doorTriggerLayers) == 0)
            {
                return;
            }

            if (_playersInTrigger == 1)
            {
                // Start the close timer (which may be stopped).
                _closeTimer = StartCoroutine(RunCloseEventually(DoorWaitCloseTime));
            }

            _playersInTrigger = Math.Max(_playersInTrigger - 1, 0);
        }

        private IEnumerator RunCloseEventually(float time)
        {
            yield return new WaitForSeconds(time);
            SetOpen(false);
        }

        [Server]
        private void SetOpen(bool open)
        {
            Subsystems.Get<AtmosEnvironmentSubSystem>().ChangeState(transform.position, open ? AtmosState.Active : AtmosState.Blocked);
            _animator.SetBool(OpenId, open);
        }
    }
}
