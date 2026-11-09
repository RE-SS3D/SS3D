using FishNet.Component.Animating;
using SS3D.Core;
using SS3D.Systems.Entities.Data;
using SS3D.Systems.Entities.Humanoid.Body;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Sole owner of humanoid body presentation (locomotion / collapsed / dead): ragdoll physics,
    /// animator suppress, and movement gating. Health and combat write intent via
    /// <see cref="ServerSetPresentation"/>; other systems read <see cref="Presentation"/>.
    /// </summary>
	public partial class Ragdoll : NetworkBehaviour
	{
        [SerializeField]
		private Transform _armatureRoot;
        private Transform _hips;
        private Transform _character;
        private Animator _animator; 
        private NetworkAnimator _networkAnimator; 
        private bool _networkAnimatorInitiallyEnabled;
        private HumanoidPredictedMovement _predictedMovement;
        private bool _predictedMovementInitiallyEnabled;
        private HumanoidLivingController _humanoidLivingController; 
        private CharacterController _characterController; 
        private Transform[] _ragdollParts;
        /// <summary>
        /// Every humanoid bone, blended from the ragdoll into the stand-up's first frame. Wider than the ragdoll
        /// parts: the chest, neck, hands and feet have no rigidbody but would otherwise snap.
        /// </summary>
        private Transform[] _blendBones;
        /// <summary>
        /// If knockdown is supposed to expire
        /// </summary>
        private bool _isKnockdownTimed;
        /// <summary>
        /// How many seconds are left before the ragdoll expires
        /// </summary>
        private float _knockdownTimer; 
        private float _elapsedResetBonesTime; 
        /// <summary>
        /// Determines how much higher than the lowest point character will be during AlignToHips(). This var prevent character from getting stuck in the floor 
        /// </summary>
        private const float AlignmentYDelta = 0.0051f;
        private enum RagdollState
        {
            Walking,
            Ragdoll,
            BonesReset,
            StandingUp
        }
        private RagdollState _currentState;
        private class BoneTransform
        {
            public Vector3 Position;
            public Quaternion Rotation;
        }
        /// <summary>
        /// Bones Transforms (position and rotation) in the first frame of StandUp animation
        /// </summary>
        private BoneTransform[] _standUpBones;
        /// <summary>
        /// Bones Transforms (position and rotation) during the Ragdoll state
        /// </summary>
        private BoneTransform[] _ragdollBones;

        [SyncVar(OnChange = nameof(OnSyncPresentation))]
        private BodyPresentationState _presentation = BodyPresentationState.Locomotion;

        /// <summary>Replicated presentation authority. Prefer this over inventing collapse rules elsewhere.</summary>
        public BodyPresentationState Presentation => _presentation;

        /// <summary>True when presentation is not locomotion (collapsed or dead).</summary>
        public bool IsKnockedDown => _presentation != BodyPresentationState.Locomotion;

        /// <summary>
        /// True from recovery until movement comes back: the blend into the stand-up clip and the clip up to
        /// <see cref="GetUpAnimation.BlendOutStart"/>. Presentation is already locomotion, so a hit knocks down again.
        /// </summary>
        public bool IsGettingUp => _currentState == RagdollState.BonesReset || _currentState == RagdollState.StandingUp;

        public event Action<bool> OnKnockdownChanged;
        /// <summary>
        /// Which stand-up clip this get-up plays. Read from the hips on each peer when the get-up starts:
        /// observers hold the synced ragdoll pose, so they agree with the owner without a SyncVar.
        /// </summary>
        private GetUpAnimation.Clip _getUp = GetUpAnimation.FaceUp;
        /// <summary>Frame the stand-up state was played; its state info is stale until the Animator updates.</summary>
        private int _standUpPlayFrame;

        [SerializeField]
        private byte _ragdollPartSyncInterval;

        private bool _ragdollPartsCached;
        /// <summary>
        /// Death corpses must stay down — ownership/network teardown can disable this component
        /// and must not call Recover() back into a walking pose.
        /// </summary>
        private bool _deathRagdoll;
        private bool _lastNotifiedKnockedDown;

        private void Awake()
        {
            CacheRagdollParts();
            // Animator drives bones during locomotion. Bone NetworkTransforms default to
            // syncing in the prefab and will overwrite muscle poses until network init runs.
            ToggleKinematic(true);
            ToggleSyncRagdoll(false);
        }

        private void OnSyncPresentation(BodyPresentationState prev, BodyPresentationState next, bool asServer)
		{
			if (prev == next)
            {
                return;
            }

            if (next == BodyPresentationState.Dead)
            {
                _deathRagdoll = true;
                _isKnockdownTimed = false;
            }

            NotifyKnockdownIfChanged(next);
            ApplyPresentation(next, applyImpulse: false);
		}

        public override void OnStartNetwork()
		{
			base.OnStartNetwork();
            WindBodies.Add(this);

            // Initial SyncVars may already have applied collapse and disabled the drivers.
            // Preserve the original NetworkAnimator enabled state captured by that application.
            EnsureAnimatorCached();
            _knockdownTimer = 0;
            _hips = _armatureRoot.GetChild(0);
            _character = _armatureRoot.parent;
            _currentState = RagdollState.Walking;
            CacheRagdollParts();
            ToggleKinematic(true);
            ToggleSyncRagdoll(false);

            // Late join / host: do not rely on SyncVar OnChange alone for the initial value.
            if (_presentation != BodyPresentationState.Locomotion)
            {
                if (_presentation == BodyPresentationState.Dead)
                {
                    _deathRagdoll = true;
                }

                enabled = true;
                ApplyPresentation(_presentation, applyImpulse: false);
            }
        }

        private void CacheRagdollParts()
        {
            if (_ragdollPartsCached)
            {
                return;
            }

            _ragdollParts = (from part in GetComponentsInChildren<RagdollPart>(true)
                select part.transform).ToArray();
            Animator animator = GetComponent<Animator>();
            _blendBones = animator != null && animator.isHuman
                ? Enumerable.Range(0, (int)HumanBodyBones.LastBone)
                    .Select(bone => animator.GetBoneTransform((HumanBodyBones)bone))
                    .Where(bone => bone != null)
                    .ToArray()
                : _ragdollParts;
            _standUpBones = new BoneTransform[_blendBones.Length];
            _ragdollBones = new BoneTransform[_blendBones.Length];

            for (int boneIndex = 0; boneIndex < _blendBones.Length; boneIndex++)
            {
                _standUpBones[boneIndex] = new();
                _ragdollBones[boneIndex] = new();
            }

            _ragdollPartsCached = true;
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);

            CacheRagdollParts();

            if (IsOwner)
            {
                foreach (Transform part in _ragdollParts)
                {
                    part.GetComponent<NetworkTransform>().SetInterval(_ragdollPartSyncInterval);
                }
            }

            // Reclaim does not change the presentation SyncVar. Reapply its physics authority
            // when ownership changes, otherwise the new owner inherits kinematic observer bones.
            if (_hips != null && _presentation != BodyPresentationState.Locomotion)
            {
                ApplyPresentation(_presentation, applyImpulse: false);
            }
        }

        private void OnDisable()
        {
            // Do not Recover() here. Ownership transfer and NetworkBehaviour teardown during
            // death disable this component and would stand the corpse back up into a walk cycle.
        }

        private void Update()
		{
            if (IsServer && _isKnockdownTimed && IsKnockedDown && !_deathRagdoll)
            {
                _knockdownTimer -= Time.deltaTime;
                if (_knockdownTimer <= 0 && (!_windKnockdown || HasFloorSupport()))
                {
                    // Must call ServerRecover — Recover() is a ServerRpc and is a no-op from server.
                    ServerRecover();
                }
            }
            TickWind(Time.deltaTime);
            TickBlastShove(Time.deltaTime);
            switch (_currentState)
            {
                case RagdollState.Walking:
                    WalkingBehavior();
                    break;
                case RagdollState.Ragdoll:
                    RagdollBehavior();
                    break;
                case RagdollState.BonesReset:
                    BonesResetBehavior();
                    break;
                case RagdollState.StandingUp:
                    StandingUpBehavior();
                    break;
            }
        }

        private void WalkingBehavior() { }

        /// <summary>
        /// Server write of presentation authority. Dead is sticky. Do not gate on <c>enabled</c> —
        /// Human.prefab ships this component disabled until collapse needs Update/AlignToHips.
        /// </summary>
        [Server]
        public void ServerSetPresentation(BodyPresentationState state, bool timed = false, float addSeconds = 0f)
        {
            if (_presentation == BodyPresentationState.Dead && state != BodyPresentationState.Dead)
            {
                return;
            }

            if (state == BodyPresentationState.Dead)
            {
                _deathRagdoll = true;
                _isKnockdownTimed = false;
                _knockdownTimer = 0f;
            }
            else if (state == BodyPresentationState.Collapsed)
            {
                if (timed)
                {
                    _isKnockdownTimed = true;
                    _knockdownTimer += addSeconds;
                }
                else
                {
                    _isKnockdownTimed = false;
                }
            }
            else
            {
                if (_deathRagdoll)
                {
                    return;
                }

                _isKnockdownTimed = false;
                _knockdownTimer = 0f;
                _windKnockdown = false;
            }

            EnsureAnimatorCached();
            // Prefab may ship disabled; AlignToHips / timed recover need Update.
            enabled = true;

            BodyPresentationState previous = _presentation;
            bool applyImpulse = state == BodyPresentationState.Collapsed
                && previous == BodyPresentationState.Locomotion;

            // Assign even when unchanged so death can reinforce an already-collapsed body.
            // FishNet may skip OnChange when the value is unchanged or on server assign.
            if (previous != state)
            {
                _presentation = state;
                NotifyKnockdownIfChanged(state);
            }

            ApplyPresentation(state, applyImpulse);
        }

        private void NotifyKnockdownIfChanged(BodyPresentationState state)
        {
            bool nextKnocked = state != BodyPresentationState.Locomotion;
            if (_lastNotifiedKnockedDown == nextKnocked)
            {
                return;
            }

            _lastNotifiedKnockedDown = nextKnocked;
            OnKnockdownChanged?.Invoke(nextKnocked);
        }
        
        /// <summary>
        /// Permanent knockdown for death. Thin wrapper over <see cref="ServerSetPresentation"/>.
        /// </summary>
        [Server]
        public void ServerDeathRagdoll()
        {
            ServerSetPresentation(BodyPresentationState.Dead);
        }

        /// <summary>
        /// Knockdown that does not expire until Recover(). Safe to call from server code
        /// (admin). Prefer this over the ServerRpc from server authority paths.
        /// </summary>
        [Server]
        public void ServerKnockdownTimeless()
        {
            ServerSetPresentation(BodyPresentationState.Collapsed);
        }

        /// <summary>
        /// Client-owned request for timeless knockdown.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void KnockdownTimeless()
        {
            ServerKnockdownTimeless();
        }

        /// <summary>
        /// Knockdown the character for some time.
        /// </summary>
        [Server]
        public void ServerKnockdown(float seconds)
        {
            ServerSetPresentation(BodyPresentationState.Collapsed, timed: true, addSeconds: seconds);
        }

        /// <summary>
        /// Timed knockdown that keeps the longer of the current and the new time instead of stacking,
        /// so repeated hits keep a body down without chaining it there. An untimed collapse stays untimed.
        /// </summary>
        [Server]
        public void ServerKnockdownAtLeast(float seconds)
        {
            if (!IsKnockedDown)
            {
                ServerKnockdown(seconds);
            }
            else if (_isKnockdownTimed)
            {
                _knockdownTimer = Mathf.Max(_knockdownTimer, seconds);
            }
        }

        /// <summary>
        /// Client-owned request for timed knockdown.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void Knockdown(float seconds)
        {
            ServerKnockdown(seconds);
        }

        private void EnsureAnimatorCached()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            if (_humanoidLivingController == null)
            {
                _humanoidLivingController = GetComponent<HumanoidLivingController>();
            }

            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            if (_networkAnimator == null)
            {
                _networkAnimator = GetComponent<NetworkAnimator>();
                _networkAnimatorInitiallyEnabled = _networkAnimator != null && _networkAnimator.enabled;
            }

            if (_predictedMovement == null)
            {
                _predictedMovement = GetComponent<HumanoidPredictedMovement>();
                _predictedMovementInitiallyEnabled = _predictedMovement != null && _predictedMovement.enabled;
            }
        }

        private void ApplyPresentation(BodyPresentationState state, bool applyImpulse)
        {
            EnsureAnimatorCached();
            CacheRagdollParts();
            // Prefab may ship disabled; Update/AlignToHips must run on all peers while down.
            enabled = true;

            if (state == BodyPresentationState.Locomotion)
            {
                if (_deathRagdoll)
                {
                    return;
                }

                if (_currentState == RagdollState.Ragdoll
                    || _currentState == RagdollState.BonesReset
                    || _currentState == RagdollState.StandingUp)
                {
                    BonesReset();
                }
                else
                {
                    EnableAnimationDrivers();
                    ToggleController(true);
                    ToggleAnimator(true);
                    ToggleKinematic(true);
                    ToggleSyncRagdoll(false);
                    _currentState = RagdollState.Walking;
                }

                return;
            }

            // Collapsed or Dead
            if (state == BodyPresentationState.Dead)
            {
                _deathRagdoll = true;
                _isKnockdownTimed = false;
            }

            ApplyCollapseVisuals();

            if (applyImpulse
                && IsRagdollPhysicsAuthority()
                && _humanoidLivingController != null
                && _ragdollParts != null)
            {
                Vector3 movement = _humanoidLivingController.TargetMovement * 3f;
                if (movement.sqrMagnitude > 0.01f)
                {
                    foreach (Transform part in _ragdollParts)
                    {
                        part.GetComponent<Rigidbody>().AddForce(movement, ForceMode.VelocityChange);
                    }
                }
            }
        }

        private void DisableAnimationDrivers()
        {
            if (TryGetComponent(out AnimationOrchestrator orchestrator))
            {
                orchestrator.SetPosingSuppressed(true);
                orchestrator.enabled = false;
            }

            if (TryGetComponent(out HumanoidBodyStateMachine bodyState))
            {
                bodyState.enabled = false;
            }
        }

        private void EnableAnimationDrivers()
        {
            if (_deathRagdoll)
            {
                return;
            }

            if (TryGetComponent(out AnimationOrchestrator orchestrator))
            {
                orchestrator.SetPosingSuppressed(false);
                orchestrator.enabled = true;
            }

            if (TryGetComponent(out HumanoidBodyStateMachine bodyState))
            {
                bodyState.enabled = true;
            }
        }

        /// <summary>
        /// Force collapsed pose. Called only from <see cref="ApplyPresentation"/>.
        /// </summary>
        private void ApplyCollapseVisuals()
        {
            EnsureAnimatorCached();
            CacheRagdollParts();
            _currentState = RagdollState.Ragdoll;
            ToggleSyncRagdoll(true);
            ToggleController(false);
            ToggleAnimator(false);
            DisableAnimationDrivers();
            // Observers must stay kinematic while bone NetworkTransforms receive — dual write twitches.
            // Death: only server simulates (corpse unowned after SwapMinds). Living knockdown: owner.
            ToggleKinematic(!IsRagdollPhysicsAuthority());
            UpdateRagdollGravity();
        }

        /// <summary>
        /// Who runs non-kinematic ragdoll physics (and sends bone NetworkTransforms).
        /// </summary>
        private bool IsRagdollPhysicsAuthority()
        {
            if (_deathRagdoll)
            {
                return IsServer;
            }

            // Mindless bodies (combat dummies) have no owning client to simulate a living
            // knockdown; without a server fallback the bones stay kinematic and the body
            // freezes in its last animated pose. Observers must still stay kinematic.
            if (!Owner.IsValid)
            {
                return IsServer;
            }

            return IsOwner;
        }

        private void RagdollBehavior()
        {
            // Owner aligns living ragdolls; server aligns death corpses (unowned after mind clear).
            if (!IsRagdollPhysicsAuthority())
            {
                return;
            }

            UpdateRagdollGravity();
            AlignToHips();
        }
        private static int _floorSupportMask = -1;

        private static int FloorSupportMask
        {
            get
            {
                if (_floorSupportMask == -1)
                {
                    _floorSupportMask = ~LayerMask.GetMask(
                        "Characters", "BodyParts", "Items", "UI", "TransparentFX", "Ignore Raycast", StationBounds.CeilingLayerName);
                }

                return _floorSupportMask;
            }
        }

        private bool HasFloorSupport()
        {
            Vector3 position = _hips != null ? _hips.position : transform.position;
            HumanoidSupportState support = HumanoidSpaceSupport.GetSupportAt(position);
            if (support != HumanoidSupportState.Unknown)
            {
                return support == HumanoidSupportState.Supported;
            }

            // Pure-client occupancy can lag behind the floor mesh. An already-floating body
            // must retain zero gravity until support is confirmed, including after death.
            if (TryGetComponent(out HumanoidBodyStateMachine bodyState) && bodyState.Snapshot.IsFloating)
            {
                return false;
            }

            return TryGetFloorBelow(position, out _);
        }

        private static bool TryGetFloorBelow(Vector3 position, out RaycastHit hit) =>
            Physics.Raycast(position + Vector3.up * 0.05f, Vector3.down, out hit, 2f,
                FloorSupportMask, QueryTriggerInteraction.Ignore);

        private void UpdateRagdollGravity()
        {
            if (!IsRagdollPhysicsAuthority())
            {
                return;
            }

            bool useGravity = HasFloorSupport();
            foreach (Transform part in _ragdollParts)
            {
                Rigidbody body = part.GetComponent<Rigidbody>();
                if (body.useGravity && !useGravity && !body.isKinematic)
                {
                    // Do not carry deck-settling fall speed into planar space float.
                    Vector3 velocity = body.linearVelocity;
                    velocity.y = 0f;
                    body.linearVelocity = velocity;
                }

                body.useGravity = useGravity;
            }
        }

        /// <summary>
        /// Adjust player's position and rotation. Character's x and z coords equals hips coords, y is at lowest positon.
        /// Character's y rotation is aligned with hips forwards direction.
        /// </summary>
        private void AlignToHips()
        {
            bool facingDown = IsHipsFacingDown();

            Vector3 originalHipsPosition = _hips.position;
            Vector3 newPosition = _hips.position;
            // Get the lowest position
            Vector3 previousRootPosition = _character.position;
            if (HasFloorSupport())
            {
                if (TryGetFloorBelow(_hips.position, out RaycastHit hitInfo))
                {
                    newPosition.y = hitInfo.point.y + AlignmentYDelta;
                }
                else
                {
                    // Support is known but the ray missed. Never lift the root to the hips' height:
                    // that would leave a body about a metre above the deck after it recovers, with
                    // nothing pulling it back down.
                    newPosition.y = previousRootPosition.y;
                }
            }
            _character.position = newPosition;
            _hips.position = originalHipsPosition;
            
            Vector3 desiredDirection = _hips.up * (facingDown ? 1 : -1);
            desiredDirection.y = 0;
            desiredDirection.Normalize();
            Quaternion originalHipsRotation = _hips.rotation;
            Vector3 rotationDifference = Quaternion.FromToRotation(transform.forward, desiredDirection).eulerAngles;
            // Make sure that rotation is only around Y axis
            rotationDifference.x = 0;
            rotationDifference.z = 0;
            transform.rotation *= Quaternion.Euler(rotationDifference);
            _hips.rotation = originalHipsRotation;
        }
        /// <summary>
        /// Switch to BonesReset state and prepare for BonesResetBehavior
        /// </summary>
        private void BonesReset()
        {
            if (_deathRagdoll)
            {
                return;
            }

            _currentState = RagdollState.BonesReset;
            _elapsedResetBonesTime = 0;

            ToggleSyncRagdoll(false);
            ToggleKinematic(true);

            // Every peer blends its own bones: they are kinematic everywhere now, and observers hold the
            // last synced ragdoll pose. Without this an observer cut from the ragdoll to the clip.
            _getUp = GetUpAnimation.For(IsHipsFacingDown());
            PopulatePartsTransforms(_ragdollBones);
            Vector3 ragdollHips = _hips.position;
            Vector3 standUpHips = PopulateStandUpPartsTransforms(_standUpBones, _getUp);
            CenterRootUnderStandUpHips(ragdollHips, standUpHips);
        }

        /// <summary>
        /// The clip's first frame does not lie on the root (face up, the hips start 0.68 m behind it). Shift the
        /// root so that frame's hips land on the ragdoll's hips; otherwise the blend drags the body across the
        /// floor. The body then stands up where its feet were.
        /// </summary>
        private void CenterRootUnderStandUpHips(Vector3 ragdollHips, Vector3 standUpHips)
        {
            Vector3 offset = ragdollHips - standUpHips;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _character.position += offset;
            _hips.position = ragdollHips;
            PopulatePartsTransforms(_ragdollBones);
        }

        private bool IsHipsFacingDown() => _hips.forward.y < 0;
        /// <summary>
        /// Interpolate bones between their lates ragdoll transform and their transform at the first frame of StandUp animation
        /// </summary>
        private void BonesResetBehavior()
        {
            _elapsedResetBonesTime += Time.deltaTime;
            if (_elapsedResetBonesTime >= GetUpAnimation.BlendInSeconds)
            {
                StandUp();
            }
            
            float elapsedPercentage = Mathf.Clamp01(_elapsedResetBonesTime / GetUpAnimation.BlendInSeconds);
            for (int partIndex = 0; partIndex < _blendBones.Length; partIndex++)
            {
                _blendBones[partIndex].localPosition = Vector3.Lerp(_ragdollBones[partIndex].Position, _standUpBones[partIndex].Position, elapsedPercentage);
                _blendBones[partIndex].localRotation = Quaternion.Lerp(_ragdollBones[partIndex].Rotation, _standUpBones[partIndex].Rotation, elapsedPercentage);
            }
        }
        /// <summary>
        /// End the BonesReset state and start StandUp animation
        /// </summary>
        private void StandUp()
        {
            _currentState = RagdollState.StandingUp;
            ToggleAnimator(true);
            _animator.Play(_getUp.StateName, 0, 0);
            _standUpPlayFrame = Time.frameCount;
        }
        /// <summary>
        /// Hold control until the clip reaches <see cref="GetUpAnimation.BlendOutStart"/>. From there the
        /// Animator blends into locomotion over the rest of the clip, so a held direction walks out of it.
        /// </summary>
        private void StandingUpBehavior()
        {
            if (Time.frameCount == _standUpPlayFrame)
            {
                return;
            }

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(_getUp.StateName)
                || state.normalizedTime >= GetUpAnimation.BlendOutStart
                || _animator.IsInTransition(0))
            {
                Walk();
            }
        }
        /// <summary>
        /// Switch state to Walking
        /// </summary>
        private void Walk()
        {
            _currentState = RagdollState.Walking;
            ToggleController(true);
            EnableAnimationDrivers();
        }

        /// <summary>
        /// Copy current blend bone transforms to array
        /// </summary>
        /// <param name="partsTransforms">Array, that receives ragdoll parts positions</param>
        private void PopulatePartsTransforms(BoneTransform[] partsTransforms)
        {
            for (int partIndex = 0; partIndex < _blendBones.Length; partIndex++)
            {
                partsTransforms[partIndex].Position = _blendBones[partIndex].localPosition;
                partsTransforms[partIndex].Rotation = _blendBones[partIndex].localRotation;
            }
        }

        /// <summary>
        /// Copy ragdoll parts transforms in the first frame of the stand-up state to array
        /// </summary>
        /// <param name = "partsTransforms">Array, that receives ragdoll parts positions</param>
        /// <returns>World position of the hips in that frame, for the current root.</returns>
        private Vector3 PopulateStandUpPartsTransforms(BoneTransform[] partsTransforms, GetUpAnimation.Clip getUp)
        {
            BoneTransform[] originalTransforms = (BoneTransform[])_ragdollBones.Clone();
            Vector3 originalArmaturePosition = _armatureRoot.localPosition;
            Quaternion originalArmatureRotation = _armatureRoot.localRotation;
            // Evaluate the state through the Animator. These are humanoid muscle clips:
            // AnimationClip.SampleAnimation leaves the bones in the avatar's T-pose, which the blend
            // then passed through. A re-enabled Animator starts with zero layer weights, so only the
            // Base Layer stand-up pose lands on the bones.
            _animator.enabled = true;
            _animator.Play(getUp.StateName, 0, 0f);
            _animator.Update(0f);
            _animator.enabled = false;
            Vector3 originalHipsPosition = _hips.position;
            Quaternion originalHipsRotation = _hips.rotation;
            _armatureRoot.localPosition = originalArmaturePosition;
            _armatureRoot.localRotation = originalArmatureRotation;
            _hips.position = originalHipsPosition;
            _hips.rotation = originalHipsRotation;
            PopulatePartsTransforms(partsTransforms);
            
            // Move bones back to their original positions
            for (int partIndex = 0; partIndex < _blendBones.Length; partIndex++)
            {
                _blendBones[partIndex].localPosition = originalTransforms[partIndex].Position;
                _blendBones[partIndex].localRotation = originalTransforms[partIndex].Rotation;
            }

            return originalHipsPosition;
        }
        [Server]
        public void ServerRecover()
        {
            ServerSetPresentation(BodyPresentationState.Locomotion);
        }

        [ServerRpc(RequireOwnership = false)]
        public void Recover()
        {
            ServerRecover();
        }
        
		/// <summary>
		/// Switch isKinematic for each ragdoll part
		/// </summary>
		private void ToggleKinematic(bool isKinematic)
		{
            if (_ragdollParts == null)
            {
                return;
            }

			foreach (Transform part in _ragdollParts)
			{
				part.GetComponent<Rigidbody>().isKinematic = isKinematic;
			}
		}
        private void ToggleController(bool enable)
        {
            if (_humanoidLivingController != null)
            {
                _humanoidLivingController.enabled = enable;
            }

            if (_characterController != null)
            {
                // Non-owners must keep CC off so NetworkTransform can drive the server transform.
                // Enabling CC on remotes leaves them stuck at spawn for proximity (doors, etc.).
                _characterController.enabled = enable && IsOwner;
            }

            // Human.prefab ships this off. Turning it on at recovery has the server replay a remote
            // owner's inputs against a controller that is off there, logging an error every tick.
            if (_predictedMovement != null)
            {
                _predictedMovement.enabled = enable && _predictedMovementInitiallyEnabled;
            }
        }
        private void ToggleAnimator(bool enable)
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }

            // Speed=0 prevents animator from choosing Walking animations after enabling it
            if (!enable && _animator != null)
            {
                _animator.SetFloat(Animations.Humanoid.MovementSpeed, 0);
            }

            if (_animator != null)
            {
                _animator.enabled = enable;
            }
            if (_networkAnimator != null && _networkAnimatorInitiallyEnabled)
            {
                _networkAnimator.enabled = enable;
            }
        }

        /// <summary>
        /// Toggle the network transform syncing of the ragdoll parts, to save up on those sweet bytes.
        /// </summary>
        /// <param name="isActive"> true if the network transform of the ragdoll parts should sync</param>
        /// <returns></returns>
        
        private void ToggleSyncRagdoll(bool isActive)
        {
            if (_ragdollParts == null)
            {
                return;
            }

            foreach (Transform part in _ragdollParts)
            {
                NetworkTransform networkTransform = part.GetComponent<NetworkTransform>();
                networkTransform.SetSynchronizePosition(isActive);
                networkTransform.SetSynchronizeRotation(isActive);
            }
        }
    }
}
