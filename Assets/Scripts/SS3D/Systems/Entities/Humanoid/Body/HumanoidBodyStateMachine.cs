using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Core;
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

        /// <summary>Synced combat aim yaw (degrees). Used for melee connect-frame raycasts.</summary>
        public float AimYaw => _aimYaw;

        /// <summary>Synced combat aim pitch (degrees). Used for melee connect-frame raycasts.</summary>
        public float AimPitch => _aimPitch;

        /// <summary>Body-local strafe axis in gait units (walk 0.3, run 1.0); drives remote VelX.</summary>
        [SyncVar(OnChange = nameof(SyncMovementVelocity))]
        private float _movementVelX;

        /// <summary>Body-local forward axis in gait units (negative = backpedal); drives remote VelZ.</summary>
        [SyncVar(OnChange = nameof(SyncMovementVelocity))]
        private float _movementVelZ;

        [SyncVar(OnChange = nameof(SyncInjuredArmLeft))]
        private float _injuredArmLeft;

        [SyncVar(OnChange = nameof(SyncInjuredArmRight))]
        private float _injuredArmRight;

        [SyncVar(OnChange = nameof(SyncInjuredLeg))]
        private float _injuredLeg;

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

        /// <summary>Owner-side coalescing: controllers publish locomotion several times a frame; send one RPC per change.</summary>
        private Vector2 _pendingOwnerVelocity;
        private Vector2 _sentOwnerVelocity = new(float.NaN, float.NaN);
        private Vector2 _sentOwnerAim = new(float.NaN, float.NaN);
        private const float LocomotionSendEpsilon = 0.01f;
        private const float AimSendEpsilonDegrees = 0.1f;

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
                if (IsOwner)
                {
                    FlushOwnerLocomotion();
                }

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

        /// <summary>Forward-only gait (ghosts, predicted movement). Equivalent to <see cref="SetLocomotionVelocity"/>(0, speed).</summary>
        public void SetLocomotionSpeed(float speed)
        {
            SetLocomotionVelocity(0f, speed);
        }

        /// <summary>
        /// Body-local locomotion in gait units, replicated so remotes play strafes and backpedals
        /// instead of forward gait. Owner clients coalesce to one RPC per frame, only on change.
        /// </summary>
        public void SetLocomotionVelocity(float velX, float velZ)
        {
            if (IsServer)
            {
                ServerApplyLocomotionVelocity(velX, velZ);
                return;
            }

            if (IsOwner)
            {
                // SyncVar is server-authoritative; do not apply snapshot locally here.
                // AnimationOrchestrator drives VelX/VelZ immediately on the owner.
                _pendingOwnerVelocity = new Vector2(velX, velZ);
            }
        }

        /// <summary>
        /// Owner aim for combat facing and melee connect raycasts. Skips the RPC while the aim is
        /// unchanged (it used to send every frame in combat mode).
        /// </summary>
        public void SubmitAim(float aimYaw, float aimPitch)
        {
            if (!float.IsNaN(_sentOwnerAim.x)
                && Mathf.Abs(Mathf.DeltaAngle(_sentOwnerAim.x, aimYaw)) < AimSendEpsilonDegrees
                && Mathf.Abs(_sentOwnerAim.y - aimPitch) < AimSendEpsilonDegrees)
            {
                return;
            }

            _sentOwnerAim = new Vector2(aimYaw, aimPitch);
            CmdSetAim(aimYaw, aimPitch);
        }

        private void FlushOwnerLocomotion()
        {
            if (!float.IsNaN(_sentOwnerVelocity.x)
                && Mathf.Abs(_pendingOwnerVelocity.x - _sentOwnerVelocity.x) < LocomotionSendEpsilon
                && Mathf.Abs(_pendingOwnerVelocity.y - _sentOwnerVelocity.y) < LocomotionSendEpsilon)
            {
                return;
            }

            _sentOwnerVelocity = _pendingOwnerVelocity;
            CmdSetLocomotionVelocity(_pendingOwnerVelocity.x, _pendingOwnerVelocity.y);
        }

        private void ServerApplyLocomotionVelocity(float velX, float velZ)
        {
            velX = Mathf.Clamp(velX, -1f, 1f);
            velZ = Mathf.Clamp(velZ, -1f, 1f);
            if (Mathf.Abs(_movementVelX - velX) < 0.0001f && Mathf.Abs(_movementVelZ - velZ) < 0.0001f)
            {
                return;
            }

            _movementVelX = velX;
            _movementVelZ = velZ;
            ApplyLocalSnapshot();
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

        public void SetMirrorUpperBody(bool mirror)
        {
            if (_snapshot.MirrorUpperBody == mirror)
            {
                return;
            }

            _snapshot.MirrorUpperBody = mirror;
            PublishSnapshot();
        }

        public void SetInjuredArms(float left, float right)
        {
            // SyncVars are server-authoritative. Assigning on a pure client spam-logs
            // FishNet "Cannot complete operation as server when server is not active."
            if (!IsServer)
            {
                return;
            }

            float clampedLeft = Mathf.Clamp01(left);
            float clampedRight = Mathf.Clamp01(right);
            if (Mathf.Approximately(_injuredArmLeft, clampedLeft)
                && Mathf.Approximately(_injuredArmRight, clampedRight))
            {
                return;
            }

            _injuredArmLeft = clampedLeft;
            _injuredArmRight = clampedRight;
            ApplyLocalSnapshot();
        }

        public void SetInjuredLeg(float injuredLeg)
        {
            if (!IsServer)
            {
                return;
            }

            float clamped = Mathf.Clamp01(injuredLeg);
            if (Mathf.Approximately(_injuredLeg, clamped))
            {
                return;
            }

            _injuredLeg = clamped;
            ApplyLocalSnapshot();
        }

        public void SetDragging(bool isDragging)
        {
            _snapshot.IsDragging = isDragging;
            PublishSnapshot();
        }

        public void SetFloating(bool floating)
        {
            LocomotionMode nextLocomotion = floating
                ? LocomotionMode.Floating
                : (_snapshot.Locomotion == LocomotionMode.Floating
                    ? LocomotionMode.Idle
                    : _snapshot.Locomotion);

            if (_snapshot.IsFloating == floating && _snapshot.Locomotion == nextLocomotion)
            {
                return;
            }

            _snapshot.IsFloating = floating;
            _snapshot.Locomotion = nextLocomotion;
            PublishSnapshot();
        }

        [ServerRpc]
        private void CmdSetLocomotionVelocity(float velX, float velZ)
        {
            ServerApplyLocomotionVelocity(velX, velZ);
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
        public void CmdFireTrigger(AnimationTriggerId trigger, byte attackVariant, byte attackWindup)
        {
            _snapshot.ActiveTrigger = trigger;
            _snapshot.AttackVariant = (byte)(attackVariant & 0x3);
            _snapshot.AttackWindup = (byte)(attackWindup & MeleeSwingAnimation.WindupQuantizedMax);
            // Pack ActiveTrigger BEFORE bumping sequence — SyncVar OnChange rebuilds from
            // _packedSnapshot synchronously; sequence++ first wiped the trigger and remotes
            // never ConsumeTrigger (fire/reload invisible on host).
            ApplyLocalSnapshot();
            _triggerSequence++;
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
            // Pack Flinch (+ Staggered) before sequence bump — SyncTriggerSequence OnChange
            // rebuilds from packed snapshot and would wipe ActiveTrigger if sequence came first.
            _snapshot.ActiveTrigger = AnimationTriggerId.Flinch;
            if (_snapshot.State != BodyState.Staggered)
            {
                _previousBodyState = _snapshot.State;
                _snapshot.State = BodyState.Staggered;
                ApplyLocalSnapshot();
                _triggerSequence++;
                OnBodyStateChanged?.Invoke(_previousBodyState, BodyState.Staggered);
                OnCapabilitiesChanged?.Invoke(Capabilities);
            }
            else
            {
                ApplyLocalSnapshot();
                _triggerSequence++;
            }
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
                if (!Physics.CheckSphere(candidate, 0.25f, StationBounds.WithoutCeiling(~0), QueryTriggerInteraction.Ignore))
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
                return;
            }

            // Owner prediction: apply locally so Floating / gait bools hit the Animator
            // before the server SyncVar round-trip (PublishSnapshot used to no-op on clients).
            if (IsOwner)
            {
                ApplyOwnerSnapshot();
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
            // BodyStateBridge limp updates call PublishSnapshot every frame; skip posing while collapsed/dead.
            if (TryGetComponent(out Ragdoll ragdoll)
                && ragdoll.Presentation != BodyPresentationState.Locomotion)
            {
                return;
            }

            _snapshot.MovementVelX = _movementVelX;
            _snapshot.MovementVelZ = _movementVelZ;
            _snapshot.MovementSpeed = Mathf.Sqrt(_movementVelX * _movementVelX + _movementVelZ * _movementVelZ);
            _snapshot.AimYaw = _aimYaw;
            _snapshot.AimPitch = _aimPitch;
            _snapshot.InjuredArmLeft = _injuredArmLeft;
            _snapshot.InjuredArmRight = _injuredArmRight;
            _snapshot.InjuredLeg = _injuredLeg;
            _orchestrator?.ApplySnapshot(_snapshot);
            OnSnapshotChanged?.Invoke(_snapshot);
        }

        private void RebuildSnapshotFromSyncVars()
        {
            _snapshot = BodyAnimationSnapshot.Unpack(
                _packedSnapshot,
                _aimYaw,
                _aimPitch,
                _movementVelX,
                _movementVelZ,
                _injuredArmLeft,
                _injuredArmRight,
                _injuredLeg);

            if (TryGetComponent(out Ragdoll ragdoll)
                && ragdoll.Presentation != BodyPresentationState.Locomotion)
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

        private void SyncMovementVelocity(float prev, float next, bool asServer)
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

        private void SyncInjuredLeg(float prev, float next, bool asServer)
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
