using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using SS3D.Systems.Electricity;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Selection;
using SS3D.Systems.Tile;
using UnityEngine;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Opens when an authorized character is in the door volume, stays open while occupied,
    /// and closes after a delay when empty. Also offers click Open/Close and access-denied feedback.
    /// </summary>
    /// <remarks>
    /// Proximity uses spawned entity transforms against the door trigger (with padding), not
    /// CharacterController physics triggers. Remotes are NetworkTransformed on the server;
    /// CC.Move never runs for them, so OnTriggerEnter / OverlapBox against the CC miss them.
    /// </remarks>
    [RequireComponent(typeof(Selectable))]
    public class AirLockOpener : NetworkBehaviour, IDynamicTileOccupant, IInteractionTarget
    {
        /// <summary>
        /// Seconds before the door starts closing once the volume is empty.
        /// </summary>
        private const float DoorWaitCloseTime = 2.0f;

        /// <summary>
        /// Sample slightly above the feet so the point sits near the door volume centerline.
        /// </summary>
        private const float OccupantSampleHeight = 0.5f;

        /// <summary>
        /// Extra reach beyond the trigger collider. Closed door solids stop characters ~5–10cm
        /// outside the OBB, so a strict contains check never authorizes.
        /// </summary>
        private const float ProximityPadding = 0.85f;

        /// <summary>
        /// Seconds a door keeps animating off-camera after a state change. Covers the longest
        /// open/close clip (1.08 s) plus a margin.
        /// </summary>
        private const float PanelMotionTime = 1.5f;

        /// <summary>
        /// Reach past the door volume when testing for a body in the doorway. The volume already spans
        /// the panels' depth, so this only covers the body's own width at the volume's edge.
        /// </summary>
        private const float DoorwayBodyMargin = 0.1f;

        /// <summary>Seconds between doorway checks while a close waits for a body to clear it.</summary>
        private const float BlockedCloseRetryInterval = 0.25f;

        private const int DoorLightMaterialIndex = 1;
        private const int DenyBlinkPulseCount = 3;
        private const float DenyBlinkHalfPeriod = 0.22f;

        private static readonly int OpenId = Animator.StringToHash("Open");

        public static readonly Color DoorLightOpeningColor = new Color(0.07f, 1f, 0.32f);
        public static readonly Color DoorLightClosingColor = new Color(1f, 0.18f, 0.2f);
        public static readonly Color DoorLightIdleColor = Color.black;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        private AirLockDoorInteraction _cachedDoorInteraction;
        private IInteraction[] _cachedDoorInteractions;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        [SerializeField]
        private LayerMask doorTriggerLayers = -1;

        /// <summary>Optional; if unset, first trigger collider under this object is used.</summary>
        [SerializeField]
        private Collider _proximityVolume;

        [SerializeField]
        private List<MeshRenderer> _meshesToColor;

        [SerializeField]
        private List<SkinnedMeshRenderer> _skinnedMeshesToColor;

        [SyncVar(OnChange = nameof(OnOpenChanged))]
        private bool _isOpen;

        private readonly HashSet<HumanInventory> _authorizedOccupants = new();
        private readonly HashSet<HumanInventory> _deniedOccupants = new();

        /// <summary>
        /// Occupants who were in the volume when someone clicked Close. They don't reopen the door
        /// until they leave the volume; anyone else arriving still opens it.
        /// </summary>
        private readonly HashSet<HumanInventory> _closeHeldOccupants = new();
        private readonly HashSet<HumanInventory> _proximityScratch = new();
        private readonly List<HumanInventory> _deniedPruneScratch = new();

        private MaterialPropertyBlock _doorLightPropertyBlock;
        private readonly List<Material> _sharedMaterialsScratch = new(4);
        private int _doorLightColorPropertyId;
        private bool _doorLightColorPropertyResolved;
        private bool _hasDoorLightMaterialSlot;

        private AirLockAccessGate _accessGate;
        private Coroutine _closeTimer;
        private Coroutine _blockedCloseTimer;
        private Coroutine _denyBlinkTimer;
        private Coroutine _panelMotionTimer;
        private AnimatorCullingMode _restingCullingMode;
        private bool _animatedOpen;

        public ReadOnlyCollection<MeshRenderer> MeshesToColor => _meshesToColor.AsReadOnly();

        public ReadOnlyCollection<SkinnedMeshRenderer> SkinnedMeshesToColor => _skinnedMeshesToColor.AsReadOnly();

        public bool IsOpen => _isOpen;

        /// <summary>True while the door animates regardless of camera visibility, because its panels are moving.</summary>
        public bool PanelsInMotion => _panelMotionTimer != null;

        public bool IsPowered => PowerGate.IsPowered(_powerConsumer, NullConsumerPolicy.Allow);

        /// <summary>True when this door still needs a proximity pass after players leave its HashGrid neighborhood.</summary>
        internal bool NeedsEmptyProximityPass =>
            _authorizedOccupants.Count > 0 || _deniedOccupants.Count > 0 || _closeHeldOccupants.Count > 0;

        private void Awake()
        {
            if (_animator != null)
            {
                _restingCullingMode = _animator.cullingMode;
            }

            if (_proximityVolume == null)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null && colliders[i].isTrigger)
                    {
                        _proximityVolume = colliders[i];
                        break;
                    }
                }
            }
        }

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

            AirLockProximityService.Instance.Register(this);

            UpdateAnimator();
            NotifyTileStateChanged();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            UpdateAnimator();
            NotifyTileStateChanged();
        }

        public override void OnStopServer()
        {
            if (_powerConsumer != null)
            {
                _powerConsumer.OnPowerStatusUpdated -= HandlePowerStatusUpdated;
            }

            AirLockProximityService.Instance.Unregister(this);

            base.OnStopServer();
        }

        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            _cachedDoorInteraction ??= new AirLockDoorInteraction(this);
            _cachedDoorInteraction.Name = IsOpen ? "Close" : "Open";
            _cachedDoorInteractions ??= new IInteraction[] { _cachedDoorInteraction };
            return _cachedDoorInteractions;
        }

        /// <summary>
        /// Shared door-light presentation used by the animator state machine and access-denied blink.
        /// </summary>
        public void SetDoorLightColor(Color color)
        {
            if (_meshesToColor != null)
            {
                EnsureDoorLightColorProperty();
                if (_hasDoorLightMaterialSlot && _doorLightColorPropertyId != 0)
                {
                    _doorLightPropertyBlock ??= new MaterialPropertyBlock();
                    for (int i = 0; i < _meshesToColor.Count; i++)
                    {
                        MeshRenderer renderer = _meshesToColor[i];
                        if (renderer == null)
                        {
                            continue;
                        }

                        renderer.GetPropertyBlock(_doorLightPropertyBlock, DoorLightMaterialIndex);
                        _doorLightPropertyBlock.SetColor(_doorLightColorPropertyId, color);
                        renderer.SetPropertyBlock(_doorLightPropertyBlock, DoorLightMaterialIndex);
                    }
                }
            }

            if (_skinnedMeshesToColor == null)
            {
                return;
            }

            for (int i = 0; i < _skinnedMeshesToColor.Count; i++)
            {
                SkinnedMeshRenderer skinnedRenderer = _skinnedMeshesToColor[i];
                if (skinnedRenderer == null)
                {
                    continue;
                }

                if (color == DoorLightOpeningColor)
                {
                    skinnedRenderer.SetBlendShapeWeight(1, 100);
                    skinnedRenderer.SetBlendShapeWeight(2, 0);
                }
                else if (color == DoorLightClosingColor)
                {
                    skinnedRenderer.SetBlendShapeWeight(1, 0);
                    skinnedRenderer.SetBlendShapeWeight(2, 100);
                }
                else
                {
                    skinnedRenderer.SetBlendShapeWeight(1, 0);
                    skinnedRenderer.SetBlendShapeWeight(2, 0);
                }
            }
        }

        private void EnsureDoorLightColorProperty()
        {
            if (_doorLightColorPropertyResolved)
            {
                return;
            }

            _doorLightColorPropertyResolved = true;
            if (_meshesToColor == null)
            {
                return;
            }

            for (int i = 0; i < _meshesToColor.Count; i++)
            {
                MeshRenderer renderer = _meshesToColor[i];
                if (renderer == null)
                {
                    continue;
                }

                _sharedMaterialsScratch.Clear();
                renderer.GetSharedMaterials(_sharedMaterialsScratch);
                if (DoorLightMaterialIndex >= _sharedMaterialsScratch.Count)
                {
                    continue;
                }

                Material targetMaterial = _sharedMaterialsScratch[DoorLightMaterialIndex];
                if (targetMaterial == null)
                {
                    continue;
                }

                if (targetMaterial.HasProperty(BaseColorId))
                {
                    _hasDoorLightMaterialSlot = true;
                    _doorLightColorPropertyId = BaseColorId;
                    return;
                }

                if (targetMaterial.HasProperty(ColorId))
                {
                    _hasDoorLightMaterialSlot = true;
                    _doorLightColorPropertyId = ColorId;
                    return;
                }
            }
        }

        [Server]
        public void ServerPlayAccessDenied()
        {
            if (!IsPowered)
            {
                return;
            }

            if (SubSystems.TryGet(out AudioSubSystem audio))
            {
                audio.PlayAudioSource(AudioType.Sfx, AirlockAudioTrackIds.AirlockDeny, transform.position, null);
            }

            RpcPlayAccessDeniedBlink();
        }

        [Server]
        public bool TryServerOpenFromInteraction(HumanInventory inventory)
        {
            if (!IsPowered)
            {
                return false;
            }

            if (_accessGate != null && !_accessGate.TryAuthorizeInventory(inventory, out _))
            {
                ServerPlayAccessDenied();
                return false;
            }

            _closeHeldOccupants.Clear();
            CancelCloseTimer();
            SetOpen(true);
            ScheduleCloseAfterDelay();
            return true;
        }

        /// <summary>Opens the door with no close timer, bypassing access. Debug/automation only (door).</summary>
        [Server]
        public bool ServerDebugHoldOpen()
        {
            if (!IsPowered)
            {
                return false;
            }

            _closeHeldOccupants.Clear();
            CancelCloseTimer();
            SetOpen(true);
            return true;
        }

        /// <summary>
        /// Closes the door and keeps it closed for everyone currently in the doorway (and
        /// <paramref name="closer"/>), until they step out of the volume.
        /// </summary>
        [Server]
        public void ServerCloseFromInteraction(HumanInventory closer = null)
        {
            CancelCloseTimer();
            _closeHeldOccupants.Clear();
            _closeHeldOccupants.UnionWith(_authorizedOccupants);
            if (closer != null)
            {
                _closeHeldOccupants.Add(closer);
            }

            SetOpen(false);
        }

        /// <summary>Called by <see cref="AirLockProximityService"/> for doors near players (or needing an empty pass).</summary>
        [Server]
        internal void ServerUpdateProximityFromService(IReadOnlyList<Entity> spawnedPlayers)
        {
            ServerUpdateProximity(spawnedPlayers);
        }

        [Server]
        private void ServerUpdateProximity(IReadOnlyList<Entity> spawnedPlayers)
        {
            if (_proximityVolume == null)
            {
                return;
            }

            if (!IsPowered)
            {
                if (_authorizedOccupants.Count > 0)
                {
                    _authorizedOccupants.Clear();
                    ScheduleCloseAfterDelay();
                }

                _deniedOccupants.Clear();
                _closeHeldOccupants.Clear();
                return;
            }

            if (spawnedPlayers == null)
            {
                return;
            }

            bool wasEmpty = _authorizedOccupants.Count == 0;
            _authorizedOccupants.Clear();
            _proximityScratch.Clear();

            for (int i = 0; i < spawnedPlayers.Count; i++)
            {
                Entity entity = spawnedPlayers[i];
                if (entity == null)
                {
                    continue;
                }

                if ((doorTriggerLayers.value & (1 << entity.gameObject.layer)) == 0)
                {
                    continue;
                }

                Vector3 sample = entity.transform.position + Vector3.up * OccupantSampleHeight;
                if (!IsPointInProximity(sample))
                {
                    continue;
                }

                if (!entity.TryGetHumanInventory(out HumanInventory inventory))
                {
                    continue;
                }

                _proximityScratch.Add(inventory);

                if (_accessGate != null &&
                    !_accessGate.TryAuthorizeInventory(inventory, out _))
                {
                    if (_deniedOccupants.Add(inventory))
                    {
                        ServerPlayAccessDenied();
                    }

                    continue;
                }

                _deniedOccupants.Remove(inventory);
                _authorizedOccupants.Add(inventory);
            }

            PruneDeniedOccupants();
            _closeHeldOccupants.RemoveWhere(held => held == null || !_proximityScratch.Contains(held));

            bool isEmpty = _authorizedOccupants.Count == 0;
            if (!isEmpty)
            {
                CancelCloseTimer();

                // A click Close holds for whoever was in the doorway then; a newcomer still opens it,
                // or keeps it open if the close is still waiting for the doorway to clear.
                if (!_closeHeldOccupants.IsSupersetOf(_authorizedOccupants))
                {
                    if (!_isOpen)
                    {
                        _closeHeldOccupants.Clear();
                        SetOpen(true);
                    }
                    else
                    {
                        CancelBlockedClose();
                    }
                }
            }
            else if (!wasEmpty)
            {
                ScheduleCloseAfterDelay();
            }
        }

        private void PruneDeniedOccupants()
        {
            _deniedPruneScratch.Clear();
            foreach (HumanInventory inventory in _deniedOccupants)
            {
                if (inventory == null || !_proximityScratch.Contains(inventory))
                {
                    _deniedPruneScratch.Add(inventory);
                }
            }

            for (int i = 0; i < _deniedPruneScratch.Count; i++)
            {
                _deniedOccupants.Remove(_deniedPruneScratch[i]);
            }
        }

        private bool IsPointInProximity(Vector3 worldPoint)
        {
            // ClosestPoint returns the point when inside; outside it returns the surface.
            // Padding covers the gap between the closed door mesh and the trigger volume.
            Vector3 closest = _proximityVolume.ClosestPoint(worldPoint);
            return (closest - worldPoint).sqrMagnitude <= ProximityPadding * ProximityPadding;
        }

        private IEnumerator RunCloseEventually(float time)
        {
            yield return new WaitForSeconds(time);
            _closeTimer = null;
            SetOpen(false);
        }

        private IEnumerator RunDenyBlink()
        {
            for (int pulse = 0; pulse < DenyBlinkPulseCount; pulse++)
            {
                SetDoorLightColor(DoorLightClosingColor);
                yield return new WaitForSeconds(DenyBlinkHalfPeriod);
                SetDoorLightColor(DoorLightIdleColor);
                yield return new WaitForSeconds(DenyBlinkHalfPeriod);
            }

            _denyBlinkTimer = null;
        }

        [ObserversRpc(RunLocally = true)]
        private void RpcPlayAccessDeniedBlink()
        {
            if (_denyBlinkTimer != null)
            {
                StopCoroutine(_denyBlinkTimer);
            }

            _denyBlinkTimer = StartCoroutine(RunDenyBlink());
        }

        [Server]
        private void SetOpen(bool open)
        {
            if (open && !IsPowered)
            {
                return;
            }

            if (open)
            {
                CancelBlockedClose();
            }
            else if (_isOpen && IsDoorwayBlocked())
            {
                // The panels' colliders slide across the doorway. Closing them on a body made the
                // character controller push it out sideways, into the wall beside the door, where it
                // could walk out anywhere. Wait until the doorway is clear instead.
                _blockedCloseTimer ??= StartCoroutine(RunBlockedClose());
                return;
            }

            _isOpen = open;
            UpdateAnimator();
        }

        private IEnumerator RunBlockedClose()
        {
            WaitForSeconds wait = new(BlockedCloseRetryInterval);
            do
            {
                yield return wait;
            }
            while (IsDoorwayBlocked());

            _blockedCloseTimer = null;
            _isOpen = false;
            UpdateAnimator();
        }

        private void CancelBlockedClose()
        {
            if (_blockedCloseTimer == null)
            {
                return;
            }

            StopCoroutine(_blockedCloseTimer);
            _blockedCloseTimer = null;
        }

        /// <summary>
        /// True while any player body, authorized or not, stands where the closed panels would be.
        /// </summary>
        private bool IsDoorwayBlocked()
        {
            if (_proximityVolume == null || !SubSystems.TryGet(out EntitySubSystem entities))
            {
                return false;
            }

            IReadOnlyList<Entity> players = entities.SpawnedPlayers;
            for (int i = 0; i < players.Count; i++)
            {
                Entity entity = players[i];
                if (entity != null && IsInDoorway(entity.transform.position + Vector3.up * OccupantSampleHeight))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInDoorway(Vector3 worldPoint)
        {
            if (_proximityVolume is BoxCollider box)
            {
                Vector3 local = box.transform.InverseTransformPoint(worldPoint) - box.center;
                return IsInsideBox(local, box.size * 0.5f, DoorwayBodyMargin);
            }

            Vector3 closest = _proximityVolume.ClosestPoint(worldPoint);
            return (closest - worldPoint).sqrMagnitude <= DoorwayBodyMargin * DoorwayBodyMargin;
        }

        /// <summary>Whether a point in a box's local space lies within its half extents plus a margin.</summary>
        private static bool IsInsideBox(Vector3 localPoint, Vector3 halfExtents, float margin)
        {
            return Mathf.Abs(localPoint.x) <= halfExtents.x + margin
                && Mathf.Abs(localPoint.y) <= halfExtents.y + margin
                && Mathf.Abs(localPoint.z) <= halfExtents.z + margin;
        }

        private void OnOpenChanged(bool _, bool __, bool asServer)
        {
            UpdateAnimator();
            NotifyTileStateChanged();
        }

        private void UpdateAnimator()
        {
            if (_animator == null)
            {
                return;
            }

            _animator.SetBool(OpenId, _isOpen);
            if (_animatedOpen != _isOpen)
            {
                _animatedOpen = _isOpen;
                AnimatePanelMotion();
            }
        }

        /// <summary>
        /// The solid colliders ride on the animated panels, so a culled animator leaves an open door
        /// physically shut, and a dedicated server never has a camera to un-cull it. Idle doors stay
        /// culled because big maps hold too many of them to animate all the time.
        /// </summary>
        private void AnimatePanelMotion()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (_panelMotionTimer != null)
            {
                StopCoroutine(_panelMotionTimer);
            }

            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _panelMotionTimer = StartCoroutine(RunPanelMotion());
        }

        private IEnumerator RunPanelMotion()
        {
            yield return new WaitForSeconds(PanelMotionTime);
            _animator.cullingMode = _restingCullingMode;
            _panelMotionTimer = null;
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
            if (_closeTimer != null)
            {
                return;
            }

            _closeTimer = StartCoroutine(RunCloseEventually(DoorWaitCloseTime));
        }

        private void CancelCloseTimer()
        {
            if (_closeTimer == null)
            {
                return;
            }

            StopCoroutine(_closeTimer);
            _closeTimer = null;
        }
    }
}
