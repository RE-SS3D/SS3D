using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities.Humanoid;
using System;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Central authority for humanoid body state, capabilities, and animation intents.
    /// Resolves #1060 design: state drives animation and movement permissions.
    /// </summary>
    [RequireComponent(typeof(AnimationOrchestrator))]
    public class HumanoidBodyStateMachine : NetworkActor
    {
        public event Action<BodyAnimationSnapshot> OnSnapshotChanged;
        public event Action<BodyState, BodyState> OnBodyStateChanged;
        public event Action<BodyCapabilities> OnCapabilitiesChanged;

        [SyncVar(OnChange = nameof(SyncPackedSnapshot))]
        private uint _packedSnapshot;

        [SyncVar(OnChange = nameof(SyncAimYaw))]
        private float _aimYaw;

        [SyncVar(OnChange = nameof(SyncAimPitch))]
        private float _aimPitch;

        [SyncVar(OnChange = nameof(SyncMovementSpeed))]
        private float _movementSpeed;

        [SyncVar(OnChange = nameof(SyncInjuredArmLeft))]
        private float _injuredArmLeft;

        [SyncVar(OnChange = nameof(SyncInjuredArmRight))]
        private float _injuredArmRight;

        [SyncVar(OnChange = nameof(SyncTriggerSequence))]
        private byte _triggerSequence;

        [SerializeField] private float _staggerDuration = 0.3f;
        [SerializeField] private float _seatProximityDistance = 0.5f;
        [SerializeField] private float _seatFacingAngle = 45f;

        private BodyAnimationSnapshot _snapshot = BodyAnimationSnapshot.Default;
        private BodyState _previousBodyState = BodyState.Locomotion;
        private float _staggerTimer;
        private Transform _seatAnchor;
        private AnimationOrchestrator _orchestrator;
        private Ragdoll _ragdoll;

        public BodyAnimationSnapshot Snapshot => _snapshot;
        public BodyCapabilities Capabilities => BodyCapabilities.ForState(_snapshot.State);
        public HumanoidCombatMode CombatMode => _snapshot.CombatMode;
        public byte TriggerSequence => _triggerSequence;

        protected override void OnAwake()
        {
            base.OnAwake();
            _orchestrator = GetComponent<AnimationOrchestrator>();
            _ragdoll = GetComponent<Ragdoll>();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            RebuildSnapshotFromSyncVars();
            if (_ragdoll != null)
            {
                _ragdoll.OnKnockdownChanged += HandleRagdollChanged;
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            if (_ragdoll != null)
            {
                _ragdoll.OnKnockdownChanged -= HandleRagdollChanged;
            }
        }

        private void HandleRagdollChanged(bool isKnockedDown)
        {
            if (isKnockedDown)
            {
                SetBodyState(BodyState.Ragdoll);
            }
            else if (_snapshot.State == BodyState.Ragdoll)
            {
                SetBodyState(BodyState.Locomotion);
            }
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            AddHandle(Coimbra.Services.PlayerLoopEvents.UpdateEvent.AddListener(HandleUpdate));
        }

        private void HandleUpdate(ref Coimbra.Services.Events.EventContext context, in Coimbra.Services.PlayerLoopEvents.UpdateEvent updateEvent)
        {
            if (!IsServer)
            {
                return;
            }

            if (_snapshot.State == BodyState.Staggered)
            {
                _staggerTimer -= Time.deltaTime;
                if (_staggerTimer <= 0f)
                {
                    SetBodyState(BodyState.Locomotion);
                }
            }
        }

        public void SetLocomotionSpeed(float speed)
        {
            if (IsServer)
            {
                _movementSpeed = speed;
                ApplyLocalSnapshot();
                return;
            }

            if (IsOwner)
            {
                // SyncVar is server-authoritative; do not apply snapshot locally here.
                // AnimationOrchestrator drives Speed immediately from input on the owner.
                CmdSetMovementSpeed(speed);
            }
        }

        public void SetLocomotionMode(LocomotionMode mode)
        {
            _snapshot.Locomotion = mode;
            _snapshot.IsFloating = mode == LocomotionMode.Floating;
            PublishSnapshot();
        }

        public void SetLimpSide(LimpSide side)
        {
            _snapshot.LimpSide = side;
            _snapshot.Locomotion = side switch
            {
                LimpSide.Left => LocomotionMode.LimpLeft,
                LimpSide.Right => LocomotionMode.LimpRight,
                _ => _snapshot.Locomotion,
            };
            PublishSnapshot();
        }

        public void SetArmHold(ArmHoldPose hold)
        {
            _snapshot.ArmHold = hold;
            PublishSnapshot();
        }

        public void SetInjuredArms(float left, float right)
        {
            _injuredArmLeft = Mathf.Clamp01(left);
            _injuredArmRight = Mathf.Clamp01(right);
            if (IsServer)
            {
                ApplyLocalSnapshot();
            }
        }

        public void SetDragging(bool isDragging)
        {
            _snapshot.IsDragging = isDragging;
            PublishSnapshot();
        }

        public void SetFloating(bool floating)
        {
            _snapshot.IsFloating = floating;
            if (floating)
            {
                _snapshot.Locomotion = LocomotionMode.Floating;
            }
            PublishSnapshot();
        }

        [ServerRpc]
        public void CmdSetMovementSpeed(float speed)
        {
            _movementSpeed = speed;
            ApplyLocalSnapshot();
        }

        [ServerRpc]
        public void CmdSetCombatMode(HumanoidCombatMode mode)
        {
            _snapshot.CombatMode = mode;
            ApplyLocalSnapshot();
        }

        [ServerRpc]
        public void CmdSetAimYaw(float aimYaw)
        {
            _aimYaw = aimYaw;
            ApplyLocalSnapshot();
        }

        [ServerRpc]
        public void CmdSetAim(float aimYaw, float aimPitch)
        {
            _aimYaw = aimYaw;
            _aimPitch = Mathf.Clamp(aimPitch, -60f, 60f);
            ApplyLocalSnapshot();
        }

        [ServerRpc]
        public void CmdFireTrigger(AnimationTriggerId trigger)
        {
            _snapshot.ActiveTrigger = trigger;
            _triggerSequence++;
            ApplyLocalSnapshot();
        }

        [ServerRpc]
        public void CmdTrySit(NetworkObject seatAnchorObject)
        {
            if (seatAnchorObject == null)
            {
                return;
            }

            SeatAnchor anchor = seatAnchorObject.GetComponent<SeatAnchor>();
            if (anchor == null || !CanSitAt(anchor))
            {
                return;
            }

            _seatAnchor = anchor.SitTransform;
            _snapshot.IsSeated = true;
            SetBodyState(BodyState.Seated);
            SnapToSeat(_seatAnchor);
        }

        [ServerRpc]
        public void CmdTryStand()
        {
            if (_snapshot.State != BodyState.Seated)
            {
                return;
            }

            Vector3 exitPosition = FindSeatExitPosition();
            transform.position = exitPosition;
            _snapshot.IsSeated = false;
            _seatAnchor = null;
            SetBodyState(BodyState.Locomotion);
        }

        [Server]
        public void ApplyStagger(float duration = -1f)
        {
            _staggerTimer = duration > 0f ? duration : _staggerDuration;
            SetBodyState(BodyState.Staggered);
            _snapshot.ActiveTrigger = AnimationTriggerId.Flinch;
            _triggerSequence++;
            ApplyLocalSnapshot();
        }

        [Server]
        public void ApplyKnockback(Vector3 direction, float force)
        {
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.Move(direction.normalized * force);
            }
        }

        public bool CanPerformAction()
        {
            return Capabilities.CanUseHands && _snapshot.State != BodyState.Staggered;
        }

        private bool CanSitAt(SeatAnchor anchor)
        {
            if (_snapshot.State != BodyState.Locomotion)
            {
                return false;
            }

            Vector3 toSeat = anchor.SitTransform.position - transform.position;
            toSeat.y = 0f;
            if (toSeat.magnitude > _seatProximityDistance)
            {
                return false;
            }

            Vector3 seatForward = anchor.SitTransform.forward;
            seatForward.y = 0f;
            float angle = Vector3.Angle(transform.forward, seatForward);
            return angle <= _seatFacingAngle;
        }

        private void SnapToSeat(Transform anchor)
        {
            transform.position = anchor.position;
            transform.rotation = Quaternion.LookRotation(anchor.forward, Vector3.up);
        }

        private Vector3 FindSeatExitPosition()
        {
            if (_seatAnchor == null)
            {
                return transform.position;
            }

            Vector3[] offsets =
            {
                _seatAnchor.forward * 0.6f,
                -_seatAnchor.right * 0.6f,
                _seatAnchor.right * 0.6f,
                -_seatAnchor.forward * 0.6f,
            };

            foreach (Vector3 offset in offsets)
            {
                Vector3 candidate = _seatAnchor.position + offset;
                if (!Physics.CheckSphere(candidate, 0.25f, ~0, QueryTriggerInteraction.Ignore))
                {
                    return candidate;
                }
            }

            return _seatAnchor.position + Vector3.up * 0.5f;
        }

        private void SetBodyState(BodyState newState)
        {
            if (_snapshot.State == newState)
            {
                return;
            }

            _previousBodyState = _snapshot.State;
            _snapshot.State = newState;
            PublishSnapshot();
            OnBodyStateChanged?.Invoke(_previousBodyState, newState);
            OnCapabilitiesChanged?.Invoke(Capabilities);
        }

        private void PublishSnapshot()
        {
            if (IsServer)
            {
                ApplyLocalSnapshot();
            }
        }

        [Server]
        private void ApplyLocalSnapshot()
        {
            ApplyOwnerSnapshot();
            _packedSnapshot = _snapshot.Pack();
        }

        private void ApplyOwnerSnapshot()
        {
            // BodyStateBridge limp updates call PublishSnapshot every frame; skip posing while ragdolled.
            if (TryGetComponent(out Ragdoll ragdoll) && ragdoll.IsKnockedDown)
            {
                return;
            }

            _snapshot.MovementSpeed = _movementSpeed;
            _snapshot.AimYaw = _aimYaw;
            _snapshot.AimPitch = _aimPitch;
            _snapshot.InjuredArmLeft = _injuredArmLeft;
            _snapshot.InjuredArmRight = _injuredArmRight;
            _orchestrator?.ApplySnapshot(_snapshot);
            OnSnapshotChanged?.Invoke(_snapshot);
        }

        private void RebuildSnapshotFromSyncVars()
        {
            _snapshot = BodyAnimationSnapshot.Unpack(
                _packedSnapshot, _aimYaw, _aimPitch, _movementSpeed, _injuredArmLeft, _injuredArmRight);

            if (TryGetComponent(out Ragdoll ragdoll) && ragdoll.IsKnockedDown)
            {
                OnCapabilitiesChanged?.Invoke(Capabilities);
                return;
            }

            _orchestrator?.ApplySnapshot(_snapshot);
            OnSnapshotChanged?.Invoke(_snapshot);
            OnCapabilitiesChanged?.Invoke(Capabilities);
        }

        private void SyncPackedSnapshot(uint prev, uint next, bool asServer)
        {
            if (prev == next) return;
            RebuildSnapshotFromSyncVars();
        }

        private void SyncAimYaw(float prev, float next, bool asServer)
        {
            if (Mathf.Approximately(prev, next)) return;
            RebuildSnapshotFromSyncVars();
        }

        private void SyncAimPitch(float prev, float next, bool asServer)
        {
            if (Mathf.Approximately(prev, next)) return;
            RebuildSnapshotFromSyncVars();
        }

        private void SyncMovementSpeed(float prev, float next, bool asServer)
        {
            if (Mathf.Approximately(prev, next)) return;
            RebuildSnapshotFromSyncVars();
        }

        private void SyncInjuredArmLeft(float prev, float next, bool asServer)
        {
            if (Mathf.Approximately(prev, next)) return;
            RebuildSnapshotFromSyncVars();
        }

        private void SyncInjuredArmRight(float prev, float next, bool asServer)
        {
            if (Mathf.Approximately(prev, next)) return;
            RebuildSnapshotFromSyncVars();
        }

        private void SyncTriggerSequence(byte prev, byte next, bool asServer)
        {
            if (prev == next) return;
            RebuildSnapshotFromSyncVars();
        }
    }
}
