using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SS3D.Systems.Electricity;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Script controlling the opening and closing of airlocks.
    /// When a player get close to an airlock, open the airlock. Keep the airlock opened as long as a player
    /// is close to the opened airlock. When no player are close to it, close the airlock.
    /// </summary>
    public class AirLockOpener : NetworkBehaviour, IDynamicTileOccupant
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
        /// Authorized occupants currently in the trigger volume.
        /// </summary>
        private readonly HashSet<Collider> _authorizedOccupants = new();

        /// <summary>
        /// Coroutine to eventually close the door when no one is around.
        /// </summary>
        private Coroutine closeTimer; // Server Only

        private AirLockAccessGate _accessGate;

        [SyncVar(OnChange = nameof(OnOpenChanged))]
        private bool _isOpen;

        [SerializeField]
        private List<MeshRenderer> _meshesToColor;

        public ReadOnlyCollection<MeshRenderer> MeshesToColor => _meshesToColor.AsReadOnly();

        [SerializeField]
        private List<SkinnedMeshRenderer> _skinnedMeshesToColor;

        public ReadOnlyCollection<SkinnedMeshRenderer> SkinnedMeshesToColor => _skinnedMeshesToColor.AsReadOnly();

        public bool IsOpen => _isOpen;

        public override void OnStartServer()
        {
            base.OnStartServer();

            _accessGate = GetComponent<AirLockAccessGate>();

            if (_powerConsumer == null)
            {
                TryGetComponent(out _powerConsumer);
            }

            if (_powerConsumer != null)
            {
                _powerConsumer.OnPowerStatusUpdated += HandlePowerStatusUpdated;
            }

            NotifyTileStateChanged();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            NotifyTileStateChanged();
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

            if (_accessGate != null && !_accessGate.TryAuthorizeCollider(other, out HumanInventory _, out _))
            {
                return;
            }

            if (_authorizedOccupants.Add(other) && _authorizedOccupants.Count == 1)
            {
                if (closeTimer != null)
                {
                    StopCoroutine(closeTimer);
                    closeTimer = null;
                }

                SetOpen(true);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if(!IsServer) return;
            if ((1 << other.gameObject.layer & doorTriggerLayers) == 0) return;

            if (!_authorizedOccupants.Remove(other))
            {
                return;
            }

            if (_authorizedOccupants.Count == 0)
            {
                ScheduleCloseAfterDelay();
            }
        }

        private IEnumerator RunCloseEventually(float time)
        {
            yield return new WaitForSeconds(time);
            closeTimer = null;
            SetOpen(false);
        }

        [Server]
        private void SetOpen(bool open)
        {
            if (open && !IsPowered())
            {
                return;
            }

            _isOpen = open;
            _animator.SetBool(OpenId, open);
        }

        private void OnOpenChanged(bool _, bool __, bool asServer)
        {
            NotifyTileStateChanged();
        }

        private void NotifyTileStateChanged()
        {
            SubSystems.Get<TileSubSystem>()?.NotifyTileStateChanged(transform.position);
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
            if (_authorizedOccupants.Count > 0)
            {
                return;
            }

            // Do not restart a running timer. Power-status churn (or repeated exits) used to
            // reset the 2s delay forever so the door never closed.
            if (closeTimer != null)
            {
                return;
            }

            closeTimer = StartCoroutine(RunCloseEventually(DOOR_WAIT_CLOSE_TIME));
        }

        private bool IsPowered()
        {
            return PowerGate.IsPowered(_powerConsumer, NullConsumerPolicy.Allow);
        }
    }
}
