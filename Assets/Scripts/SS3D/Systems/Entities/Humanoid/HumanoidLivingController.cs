using FishNet.Connection;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
using SS3D.Systems.Stamina;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Controls the movement for living biped characters that use the same armature
    /// as the human model uses.
    /// </summary>
    [RequireComponent(typeof(AnimationOrchestrator))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class HumanoidLivingController : HumanoidController
    {

        [Header("Components")]
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private StaminaController _staminaController;
        private HumanHealthController _healthController;
        [SerializeField] private HumanoidPredictedMovement _predictedMovement;

        public bool IsDragging { get; set; }

        /// <summary>Planar coast while unsupported (no plenum). Used when predicted movement is disabled.</summary>
        private Vector3 _coastVelocity;

        public override void OnStartClient()
        {
            base.OnStartClient();
            _healthController = GetComponent<HumanHealthController>();
            if (_predictedMovement == null)
            {
                _predictedMovement = GetComponent<HumanoidPredictedMovement>();
            }

            ApplyCharacterControllerOwnership();
            if (!IsOwner)
            {
                return;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            ApplyCharacterControllerOwnership();
        }

        public override void OnOwnershipClient(NetworkConnection prevOwner)
        {
            base.OnOwnershipClient(prevOwner);
            ApplyCharacterControllerOwnership();
        }

        public override void OnOwnershipServer(NetworkConnection prevOwner)
        {
            base.OnOwnershipServer(prevOwner);
            ApplyCharacterControllerOwnership();
        }

        /// <summary>
        /// Only the owning peer simulates CharacterController. Remotes (including on the server)
        /// must keep it disabled so client-authoritative NetworkTransform can move the transform;
        /// an enabled CC ignores teleports and freezes server-side proximity (airlocks, etc.).
        /// </summary>
        private void ApplyCharacterControllerOwnership()
        {
            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }

            if (_characterController == null)
            {
                return;
            }

            if (!IsOwner)
            {
                _characterController.enabled = false;
                return;
            }

            if (TryGetComponent(out Ragdoll ragdoll) && ragdoll.IsKnockedDown)
            {
                _characterController.enabled = false;
                return;
            }

            _characterController.enabled = true;
        }

        /// <summary>
        /// Server packs Floating SyncVar from full-map support so pure clients / remotes see deep space.
        /// </summary>
        protected override void ServerReconcileSpaceSupport()
        {
            if (BodyStateMachine == null)
            {
                return;
            }

            if (TryGetComponent(out Ragdoll ragdoll)
                && ragdoll.Presentation != BodyPresentationState.Locomotion)
            {
                return;
            }

            HumanoidSupportState support = HumanoidSpaceSupport.GetSupportFor(this, transform.position);
            if (support == HumanoidSupportState.Unknown)
            {
                // Map/AOI not authoritative yet — never leave a stale Floating SyncVar packed.
                if (BodyStateMachine.Snapshot.IsFloating)
                {
                    BodyStateMachine.SetFloating(false);
                }

                return;
            }

            bool shouldFloat = support == HumanoidSupportState.Unsupported;
            if (BodyStateMachine.Snapshot.IsFloating != shouldFloat)
            {
                BodyStateMachine.SetFloating(shouldFloat);
            }
        }

        /// <summary>
        /// Executes the movement code and updates the IK targets
        /// </summary>
        protected override void ProcessCharacterMovement()
        {
            if (_healthController != null
                && BodyPresentationIntent.FromSnapshot(_healthController.Snapshot) != BodyPresentationState.Locomotion)
            {
                Input = Vector2.zero;
                TargetMovement = Vector3.zero;
                return;
            }

            if (TryGetComponent(out Ragdoll ragdoll)
                && ragdoll.Presentation != BodyPresentationState.Locomotion)
            {
                // Collapse disables this controller; do not Move gravity/CC while down (fights ragdoll).
                return;
            }

            bool predictedOwnsLoco = _predictedMovement != null && _predictedMovement.enabled;
            HumanoidSupportState support = HumanoidSpaceSupport.GetSupportFor(this, transform.position);
            bool coasting = ShouldCoast(support);
            // Avoid publishing walk Speed the same frame we enter space float (clears Floating via gait).
            ProcessPlayerInput(publishSpeed: !predictedOwnsLoco && !coasting);

            if (predictedOwnsLoco)
            {
                return;
            }

            // Human.prefab ships PredictedMovement disabled — this path owns living space float.
            if (TryProcessSpaceFloat(support))
            {
                return;
            }

            // Gravity only snaps onto a floor collider. Tile support ignores height, so a body that
            // fell below the deck (plenum known, floor collider not mirrored yet) would otherwise
            // read as Supported and fall forever. Invincible walkers also cross open space this way.
            if (HasPhysicalFloorUnderfoot())
            {
                _characterController.Move(Physics.gravity);
            }

            float gaitSpeed = FilterSpeed();
            if (Input.magnitude != 0)
            {
                MoveMovementTarget(Input);
                if (!IsDragging)
                {
                    if (IsCombatMode())
                    {
                        RotatePlayerToCombatAim();
                    }
                    else
                    {
                        RotatePlayerToMovement();
                    }
                }

                MovePlayer();
                PublishLocomotionVelocity(TargetMovement, gaitSpeed);
            }
            else
            {
                MovePlayer();
                MoveMovementTarget(Vector2.zero, 5);
                if (IsCombatMode() && !IsDragging)
                {
                    RotatePlayerToCombatAim();
                }

                PublishLocomotionVelocity(Vector3.zero, 0f);
            }
        }

        /// <summary>
        /// When no plenum underfoot: Floating anim, no gravity/WASD, coast last planar velocity.
        /// Unknown (client AOI lag): never enter float. Skip gravity only while no physical floor
        /// exists yet (prevents fall-through); once colliders are underfoot, walk normally even if
        /// tile occupancy is still Incomplete. Keep coasting only if SyncVar already Floating.
        /// Returns true when space float / AOI hold consumed this frame.
        /// </summary>
        private bool TryProcessSpaceFloat(HumanoidSupportState support)
        {
            bool wasFloating = BodyStateMachine != null && BodyStateMachine.Snapshot.IsFloating;

            if (support == HumanoidSupportState.Supported)
            {
                if (wasFloating)
                {
                    BodyStateMachine.SetFloating(false);
                    if (Input.magnitude < 0.01f)
                    {
                        _coastVelocity = Vector3.zero;
                    }
                }

                return false;
            }

            if (support == HumanoidSupportState.Unknown)
            {
                if (!wasFloating)
                {
                    // Tile knowledge incomplete (!HasPlenum / AOI miss). Do not float.
                    // If floor colliders already mirrored, resume normal loco — holding forever
                    // soft-locks spawn after the map is visible but occupancy still Unknown.
                    if (HasPhysicalFloorUnderfoot())
                    {
                        ApplyCharacterControllerOwnership();
                        return false;
                    }

                    // No colliders yet: no gravity (fall-through), but allow planar walk and
                    // keep the owner CharacterController enabled.
                    ApplyCharacterControllerOwnership();
                    ProcessUnknownAoiHold();
                    return true;
                }
            }
            else if (!wasFloating)
            {
                _coastVelocity = CaptureLivingCoastVelocity();
                BodyStateMachine?.SetFloating(true);
            }

            if (_coastVelocity.sqrMagnitude > 0.0001f)
            {
                _characterController.Move(_coastVelocity * Time.deltaTime);
            }

            MoveMovementTarget(Vector2.zero, 5);
            if (IsCombatMode() && !IsDragging)
            {
                RotatePlayerToCombatAim();
            }

            PublishLocomotionVelocity(Vector3.zero, 0f);
            return true;
        }

        /// <summary>
        /// AOI gap with no floor colliders: skip gravity, still accept WASD so spawn is not frozen.
        /// </summary>
        private void ProcessUnknownAoiHold()
        {
            float gaitSpeed = FilterSpeed();
            if (Input.magnitude != 0)
            {
                MoveMovementTarget(Input);
                if (!IsDragging)
                {
                    if (IsCombatMode())
                    {
                        RotatePlayerToCombatAim();
                    }
                    else
                    {
                        RotatePlayerToMovement();
                    }
                }

                MovePlayer();
                PublishLocomotionVelocity(TargetMovement, gaitSpeed);
            }
            else
            {
                MovePlayer();
                MoveMovementTarget(Vector2.zero, 5);
                if (IsCombatMode() && !IsDragging)
                {
                    RotatePlayerToCombatAim();
                }

                PublishLocomotionVelocity(Vector3.zero, 0f);
            }
        }

        /// <summary>
        /// True when a non-trigger collider sits under the capsule (tile meshes mirrored even if
        /// <see cref="HumanoidSpaceSupport"/> is still Unknown).
        /// </summary>
        private bool HasPhysicalFloorUnderfoot()
        {
            if (_characterController == null)
            {
                return false;
            }

            float probe = (_characterController.height * 0.5f) + _characterController.skinWidth + 0.2f;
            Vector3 origin = transform.position + Vector3.up * 0.05f;
            return Physics.Raycast(origin, Vector3.down, probe, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
        }

        private bool ShouldCoast(HumanoidSupportState support)
        {
            if (support == HumanoidSupportState.Unsupported)
            {
                return true;
            }

            return support == HumanoidSupportState.Unknown
                && BodyStateMachine != null
                && BodyStateMachine.Snapshot.IsFloating;
        }

        private Vector3 CaptureLivingCoastVelocity()
        {
            if (TargetMovement.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            float healthMultiplier = _healthController != null
                ? _healthController.Snapshot.MovementSpeedMultiplier
                : 1f;
            float combatFactor = 1f;
            if (IsCombatMode())
            {
                combatFactor = IsRunning ? _combatRunSpeedFactor : _combatWalkSpeedFactor;
            }

            float exertionFactor = _staminaController != null
                ? Mathf.Lerp(1f, 0.55f, _staminaController.ExertionPenalty)
                : 1f;
            float gait = FilterSpeed();
            float speed = _movementSpeed * healthMultiplier * combatFactor * exertionFactor * gait;
            return TargetMovement.normalized * speed;
        }

        protected override float FilterSpeed()
        {
            // Exhaustion no longer hard-blocks run (stamina.md §3); ExertionPenalty slows MovePlayer.
            return IsRunning ? RunAnimatorValue : WalkAnimatorValue;
        }

        /// <summary>
        /// Moves the player to the target movement
        /// </summary>
        protected override void MovePlayer()
        {
            // Health rewrite: MovementSpeedMultiplier subsumes the legacy per-foot FeetHealthFactor
            // (it already derives from LeftLeg/RightLeg zone damage). Combat scaling is orthogonal.
            float healthMultiplier = _healthController != null
                ? _healthController.Snapshot.MovementSpeedMultiplier
                : 1f;
            float combatFactor = 1f;
            // Combat walk/run clips are authored at the same cadence across stances (melee + ranged),
            // so apply slow combat speed scaling for any combat mode.
            if (IsCombatMode())
            {
                combatFactor = IsRunning ? _combatRunSpeedFactor : _combatWalkSpeedFactor;
            }

            float exertionFactor = _staminaController != null
                ? Mathf.Lerp(1f, 0.55f, _staminaController.ExertionPenalty)
                : 1f;

            _characterController.Move(
                TargetMovement * (_movementSpeed * healthMultiplier * combatFactor * exertionFactor * Time.deltaTime));
        }
    }

}
