using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            NotifyTileStateChanged();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            NotifyTileStateChanged();
        }

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
    }
}
