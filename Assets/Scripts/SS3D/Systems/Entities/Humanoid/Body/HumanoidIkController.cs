using SS3D.Core;
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// IK hooks for combat look-at and future foot placement (#937).
    /// </summary>
    public class HumanoidIkController : MonoBehaviour
    {
        private const int UpperBodyLayer = 1;
        private static readonly int ReloadRifleStateHash = Animator.StringToHash("Reload Rifle");
        private static readonly int[] AttackSwingStateHashes =
        {
            Animator.StringToHash("Attack Swing"),
            Animator.StringToHash("Attack Swing Downward"),
            Animator.StringToHash("Attack Swing Backhand"),
        };

        [SerializeField] private HumanoidRigReferences _rig;
        [SerializeField] private Transform _lookAtTarget;
        [SerializeField] private float _headLookWeight = 0.65f;
        [SerializeField] private float _torsoLookWeight = 0.45f;
        [SerializeField] private float _lookDistance = 4f;
        /// <summary>Matches combat body yaw so head look-at does not snap ahead of the torso.</summary>
        [SerializeField] private float _lookAtLerpMultiplier = 3.5f;
        [SerializeField] private float _meleeIkBlendLerp = 6f;
        [SerializeField] private float _blockedStandUpHeadroom = 1.2f;

        [Header("Ranged aim correction")]
        /// <summary>
        /// Locomotion clips twist and tilt the hips (Base layer), and the Upper Body aim pose rides on
        /// them, so the rifle drifts off target while walking. After animation, spine + chest rotate
        /// so the muzzle points at the aim point. Fraction of the error the spine takes; chest takes the rest.
        /// </summary>
        [SerializeField] private float _rangedAimSpineShare = 0.4f;
        /// <summary>Largest pitch correction applied (degrees); beyond this the torso would contort.</summary>
        [SerializeField] private float _rangedAimMaxCorrection = 60f;
        /// <summary>
        /// Largest yaw correction (degrees). Kept low so the body turning carries a fast flick and the
        /// torso does not snap ahead of it (combat body yaw is deliberately slower than the mouse).
        /// </summary>
        [SerializeField] private float _rangedAimMaxYaw = 35f;
        /// <summary>Exponential smoothing rate (per second) of the applied correction angles.</summary>
        [SerializeField] private float _rangedAimSmoothing = 14f;
        /// <summary>Correction fade rate (per second) entering/leaving Ranged and around Reload.</summary>
        [SerializeField] private float _rangedAimBlendRate = 8f;
        /// <summary>Aim points closer than this to the muzzle fall back to the aim direction (avoids flipping at point blank).</summary>
        [SerializeField] private float _rangedAimMinDistance = 1f;

        private bool _combatLookActive;
        private float _meleeAttackIkBlend;
        private float _aimYaw;
        private float _aimPitch;
        private bool _hasWorldAimPoint;
        private Vector3 _worldAimPoint;
        private Vector3 _smoothedLookTarget;
        private bool _hasSmoothedLookTarget;
        private Animator _animator;
        private Hands _hands;
        private bool _rangedAimActive;
        private float _rangedAimBlend;
        private float _appliedAimYaw;
        private float _appliedAimPitch;
        private readonly ReloadMagazineHandoff _magazineHandoff = new ReloadMagazineHandoff();
        private HumanoidBodyStateMachine _bodyStateMachine;
        private float _shotKickStartTime = float.NegativeInfinity;

        [Header("Throw twist")]
        /// <summary>
        /// Walking in Peaceful a throw keeps the legs on the walk, so the spine and chest turn the throw
        /// toward its target. Largest twist (degrees); beyond it the throw reads as over the shoulder.
        /// </summary>
        [SerializeField] private float _throwMaxTwist = 70f;
        [SerializeField] private float _throwTwistSpineShare = 0.4f;
        [SerializeField] private float _throwTwistBlendRate = 8f;

        /// <summary>Upper Body states that twist the torso toward a target: the throws and the interaction reaches.</summary>
        private static readonly int[] TwistStateHashes = BuildTwistStateHashes();
        private AnimationOrchestrator _orchestrator;
        private float _throwTwistBlend;
        private float _appliedThrowYaw;
        /// <summary>Owner's own throw yaw, used until the replicated aim catches up.</summary>
        private float _localThrowYaw;
        private float _localThrowYawUntil = float.NegativeInfinity;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_rig == null)
            {
                _rig = GetComponent<HumanoidRigReferences>();
            }

            _bodyStateMachine = GetComponent<HumanoidBodyStateMachine>();
            _orchestrator = GetComponentInParent<AnimationOrchestrator>();
        }

        /// <summary>
        /// A shot was fired (muzzle flash RPC, every peer). With a pistol hold the forearms kick up; rifles
        /// have their own fire clip, so this does nothing for them.
        /// </summary>
        public void PlayShotKick()
        {
            _shotKickStartTime = Time.time;
        }

        /// <summary>Ranged stance with a body that can pose: enables post-animation rifle aim correction.</summary>
        public void SetRangedAimActive(bool active)
        {
            _rangedAimActive = active;
        }

        public void SetCombatLookAt(bool active, float aimYaw, float aimPitch)
        {
            _combatLookActive = active;
            _aimYaw = aimYaw;
            _aimPitch = aimPitch;
            if (!active)
            {
                _hasWorldAimPoint = false;
                _hasSmoothedLookTarget = false;
                _meleeAttackIkBlend = 0f;
            }
        }

        /// <summary>
        /// Owner-side precise aim point (includes up/down). Remotes reconstruct from yaw/pitch.
        /// </summary>
        public void SetCombatAimPoint(Vector3 worldAimPoint)
        {
            _worldAimPoint = worldAimPoint;
            _hasWorldAimPoint = true;
        }

        /// <summary>
        /// Returns true if there is enough headroom to stand up from ragdoll.
        /// </summary>
        public bool HasStandUpHeadroom()
        {
            return !Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.up, _blockedStandUpHeadroom,
                StationBounds.WithoutCeiling(Physics.DefaultRaycastLayers));
        }

        /// <summary>
        /// Suggested get-up variant when headroom is blocked.
        /// </summary>
        public bool ShouldUseLowGetUp() => !HasStandUpHeadroom();

        private void OnAnimatorIK(int layerIndex)
        {
            if (_animator == null || !_animator.isHuman)
            {
                return;
            }

            // Look-at is global; only apply once (base layer pass).
            if (layerIndex != 0)
            {
                return;
            }

            Transform head = _rig != null ? _rig.Head : null;
            if (head == null)
            {
                head = _animator.GetBoneTransform(HumanBodyBones.Head);
            }

            if (head == null)
            {
                return;
            }

            if (!_combatLookActive)
            {
                _animator.SetLookAtWeight(0f);
                return;
            }

            // Swing clip owns spine/chest (upper-body mask); suppress torso look-at while Attack Swing plays.
            float swingTarget = IsUpperBodyAttackSwing() ? 1f : 0f;
            _meleeAttackIkBlend = Mathf.MoveTowards(
                _meleeAttackIkBlend,
                swingTarget,
                Time.deltaTime * _meleeIkBlendLerp);

            Vector3 desiredTarget = _hasWorldAimPoint
                ? _worldAimPoint
                : head.position + AimDirection(_aimYaw, _aimPitch) * _lookDistance;

            if (!_hasSmoothedLookTarget)
            {
                // Seed from current facing so look-at does not pop when leaving a swing.
                _smoothedLookTarget = head.position + transform.forward * _lookDistance;
                _hasSmoothedLookTarget = true;
            }

            _smoothedLookTarget = Vector3.Lerp(
                _smoothedLookTarget,
                desiredTarget,
                Time.deltaTime * _lookAtLerpMultiplier);

            if (_lookAtTarget != null)
            {
                _lookAtTarget.position = _smoothedLookTarget;
            }

            // SetLookAtWeight(global, body, head). Head stays on aim; body weight eases out during swing.
            float bodyWeight = Mathf.Lerp(_torsoLookWeight, 0f, _meleeAttackIkBlend);
            _animator.SetLookAtWeight(1f, bodyWeight, _headLookWeight);
            _animator.SetLookAtPosition(_smoothedLookTarget);
        }

        private void OnDisable()
        {
            _magazineHandoff.Restore();
        }

        private void LateUpdate()
        {
            ApplyRangedAimCorrection();
            ApplyThrowTwist();
            ApplyShotKick();
            UpdateMagazineHandoff();
        }

        /// <summary>
        /// Pitches the gun forearm (and the gripping off-hand forearm) up around the body's right axis.
        /// Both forearms point forward to the same grip, so the same world rotation keeps the hands together.
        /// Reads the replicated snapshot, so observers kick the same arm as the owner.
        /// </summary>
        private void ApplyShotKick()
        {
            float kick = PistolStanceAnimation.EvaluateShotKick(Time.time - _shotKickStartTime);
            if (kick <= 0f || _bodyStateMachine == null || _animator == null || !_animator.enabled || !_animator.isHuman)
            {
                return;
            }

            BodyAnimationSnapshot snapshot = _bodyStateMachine.Snapshot;
            if (!snapshot.PistolHold || snapshot.CombatMode != HumanoidCombatMode.Ranged)
            {
                return;
            }

            bool gunInLeft = snapshot.MirrorUpperBody;
            Vector3 right = Vector3.Cross(Vector3.up, Flatten(transform.forward));
            Quaternion pitchUp = Quaternion.AngleAxis(-kick, right.normalized);
            KickBone(gunInLeft ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm, pitchUp);
            if (!snapshot.OffHandBusy)
            {
                KickBone(gunInLeft ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm, pitchUp);
            }
        }

        private void KickBone(HumanBodyBones bone, Quaternion rotation)
        {
            Transform transformToKick = _animator.GetBoneTransform(bone);
            if (transformToKick != null)
            {
                transformToKick.rotation = rotation * transformToKick.rotation;
            }
        }

        /// <summary>
        /// Runs after the aim correction: the left hand rides the corrected chest, so the magazine
        /// must be posed from the final bone positions.
        /// </summary>
        private void UpdateMagazineHandoff()
        {
            RangedWeaponItemExtension weapon = null;
            bool canPose = _animator != null && _animator.enabled && _animator.isHuman
                && TryGetWieldedWeapon(out weapon);
            Transform leftHand = canPose ? _animator.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            _magazineHandoff.Apply(canPose ? weapon : null, leftHand, canPose ? UpperBodyReloadNormalizedTime() : -1f);
        }

        private void ApplyRangedAimCorrection()
        {
            bool wantCorrection = _rangedAimActive && _combatLookActive && _animator != null
                && _animator.enabled && _animator.isHuman && !IsUpperBodyReloading()
                && (_orchestrator == null || !_orchestrator.IsFullBodyInteractionPlaying);
            _rangedAimBlend = Mathf.MoveTowards(_rangedAimBlend, wantCorrection ? 1f : 0f, Time.deltaTime * _rangedAimBlendRate);
            if (_rangedAimBlend <= 0.001f || !_combatLookActive || !TryGetWieldedWeapon(out RangedWeaponItemExtension weapon))
            {
                _appliedAimYaw = 0f;
                _appliedAimPitch = 0f;
                return;
            }

            Transform spine = _rig != null && _rig.Spine != null ? _rig.Spine : _animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = _rig != null && _rig.Chest != null ? _rig.Chest : _animator.GetBoneTransform(HumanBodyBones.Chest);
            if (spine == null || chest == null)
            {
                return;
            }

            // Error measured on this frame's fresh animated pose, so nothing accumulates. Split into
            // yaw (world up) and pitch (body right): a free-axis FromToRotation near 180° picks an
            // arbitrary axis and rolls the torso over. Yaw is capped low so a fast flick is carried
            // by the body turning (combat yaw rate), not by the torso snapping ahead of it.
            weapon.GetMuzzleWorldPose(out Vector3 muzzlePosition, out Vector3 muzzleForward);
            Vector3 desired = DesiredAimDirection(muzzlePosition);
            float yawError = SignedPlanarAngle(muzzleForward, desired);
            float pitchError = Elevation(desired) - Elevation(muzzleForward);

            float targetYaw = Mathf.Clamp(yawError, -_rangedAimMaxYaw, _rangedAimMaxYaw) * _rangedAimBlend;
            float targetPitch = Mathf.Clamp(pitchError, -_rangedAimMaxCorrection, _rangedAimMaxCorrection) * _rangedAimBlend;
            float smoothing = 1f - Mathf.Exp(-_rangedAimSmoothing * Time.deltaTime);
            _appliedAimYaw = Mathf.Lerp(_appliedAimYaw, targetYaw, smoothing);
            _appliedAimPitch = Mathf.Lerp(_appliedAimPitch, targetPitch, smoothing);

            Vector3 right = Vector3.Cross(Vector3.up, Flatten(transform.forward));
            ApplyAimRotation(spine, right, _rangedAimSpineShare);
            ApplyAimRotation(chest, right, 1f - _rangedAimSpineShare);
        }

        private void ApplyAimRotation(Transform bone, Vector3 right, float share)
        {
            // Unity rotates +Z toward −Y for a positive angle about +X, so pitch up is negative.
            Quaternion yaw = Quaternion.AngleAxis(_appliedAimYaw * share, Vector3.up);
            Quaternion pitch = right.sqrMagnitude > 0.0001f
                ? Quaternion.AngleAxis(-_appliedAimPitch * share, right.normalized)
                : Quaternion.identity;
            bone.rotation = yaw * pitch * bone.rotation;
        }

        private static Vector3 Flatten(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        private static float SignedPlanarAngle(Vector3 from, Vector3 to)
        {
            return Vector3.SignedAngle(Flatten(from), Flatten(to), Vector3.up);
        }

        private static float Elevation(Vector3 direction)
        {
            return Mathf.Asin(Mathf.Clamp(direction.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private Vector3 DesiredAimDirection(Vector3 muzzlePosition)
        {
            Vector3 fallback = AimDirection(_aimYaw, _aimPitch);
            if (!_hasWorldAimPoint)
            {
                return fallback;
            }

            Vector3 toTarget = _worldAimPoint - muzzlePosition;
            return toTarget.magnitude < _rangedAimMinDistance ? fallback : toTarget.normalized;
        }

        private bool TryGetWieldedWeapon(out RangedWeaponItemExtension weapon)
        {
            weapon = null;
            if (_hands == null)
            {
                _hands = GetComponent<Hands>();
                if (_hands == null)
                {
                    _hands = GetComponentInParent<Hands>();
                }
            }

            return _hands != null && TwoHandedWeaponRules.TryGetWieldedRangedWeapon(_hands, out _, out weapon) && weapon != null;
        }

        private bool IsUpperBodyReloading() => UpperBodyReloadNormalizedTime() >= 0f;

        /// <summary>
        /// The yaw the current throw or interaction aims at. The owner sets it for a throw (the snapshot's AimYaw
        /// carries it to observers); every peer sets it for an interaction, whose RPC carries the yaw.
        /// </summary>
        public void SetThrowAimYaw(float yaw)
        {
            _localThrowYaw = yaw;
            _localThrowYawUntil = Time.time + 2f;
        }

        /// <summary>
        /// During a throw state, turns spine and chest toward the throw's yaw by whatever the body's facing
        /// leaves (zero when the body already faces the target), capped at <see cref="_throwMaxTwist"/>.
        /// </summary>
        private void ApplyThrowTwist()
        {
            bool canPose = _animator != null && _animator.enabled && _animator.isHuman && _bodyStateMachine != null;
            bool throwing = canPose && IsUpperBodyIn(TwistStateHashes);
            _throwTwistBlend = Mathf.MoveTowards(_throwTwistBlend, throwing ? 1f : 0f, Time.deltaTime * _throwTwistBlendRate);
            if (!canPose || _throwTwistBlend <= 0.001f)
            {
                _appliedThrowYaw = 0f;
                return;
            }

            Transform spine = _rig != null && _rig.Spine != null ? _rig.Spine : _animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = _rig != null && _rig.Chest != null ? _rig.Chest : _animator.GetBoneTransform(HumanBodyBones.Chest);
            if (spine == null || chest == null)
            {
                return;
            }

            float aimYaw = Time.time < _localThrowYawUntil ? _localThrowYaw : _bodyStateMachine.Snapshot.AimYaw;
            float error = Mathf.DeltaAngle(transform.eulerAngles.y, aimYaw);
            float target = Mathf.Clamp(error, -_throwMaxTwist, _throwMaxTwist) * _throwTwistBlend;
            _appliedThrowYaw = Mathf.Lerp(_appliedThrowYaw, target, 1f - Mathf.Exp(-_rangedAimSmoothing * Time.deltaTime));
            spine.rotation = Quaternion.AngleAxis(_appliedThrowYaw * _throwTwistSpineShare, Vector3.up) * spine.rotation;
            chest.rotation = Quaternion.AngleAxis(_appliedThrowYaw * (1f - _throwTwistSpineShare), Vector3.up) * chest.rotation;
        }

        private bool IsUpperBodyIn(int[] stateHashes)
        {
            if (_animator.layerCount <= UpperBodyLayer)
            {
                return false;
            }

            int current = _animator.GetCurrentAnimatorStateInfo(UpperBodyLayer).shortNameHash;
            int next = _animator.IsInTransition(UpperBodyLayer) ? _animator.GetNextAnimatorStateInfo(UpperBodyLayer).shortNameHash : 0;
            return System.Array.IndexOf(stateHashes, current) >= 0 || System.Array.IndexOf(stateHashes, next) >= 0;
        }

        private static int[] BuildTwistStateHashes()
        {
            var hashes = new System.Collections.Generic.List<int>();
            foreach (ThrowAnimation.Clip clip in ThrowAnimation.Variants)
            {
                hashes.Add(Animator.StringToHash(clip.StateName));
            }

            foreach (InteractionAnimation.Clip clip in InteractionAnimation.All)
            {
                if (!clip.FullBody)
                {
                    hashes.Add(Animator.StringToHash(clip.StateName));
                    if (clip.HasReturn)
                    {
                        hashes.Add(Animator.StringToHash(clip.ReturnStateName));
                    }
                }
            }

            return hashes.ToArray();
        }

        /// <summary>Normalized time of the Upper Body reload state (entering it counts), or -1 when not reloading.</summary>
        private float UpperBodyReloadNormalizedTime()
        {
            if (_animator == null || _animator.layerCount <= UpperBodyLayer)
            {
                return -1f;
            }

            if (_animator.IsInTransition(UpperBodyLayer))
            {
                AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(UpperBodyLayer);
                if (next.shortNameHash == ReloadRifleStateHash)
                {
                    return next.normalizedTime;
                }
            }

            AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(UpperBodyLayer);
            return current.shortNameHash == ReloadRifleStateHash ? current.normalizedTime : -1f;
        }

        private bool IsUpperBodyAttackSwing()
        {
            if (_animator.layerCount <= UpperBodyLayer)
            {
                return false;
            }

            if (IsAttackSwingState(_animator.GetCurrentAnimatorStateInfo(UpperBodyLayer)))
            {
                return true;
            }

            return _animator.IsInTransition(UpperBodyLayer)
                && IsAttackSwingState(_animator.GetNextAnimatorStateInfo(UpperBodyLayer));
        }

        private static bool IsAttackSwingState(AnimatorStateInfo info)
        {
            for (int i = 0; i < AttackSwingStateHashes.Length; i++)
            {
                if (info.shortNameHash == AttackSwingStateHashes[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 AimDirection(float yawDegrees, float pitchDegrees)
        {
            // pitchDegrees: positive = look up
            float yaw = yawDegrees * Mathf.Deg2Rad;
            float pitch = pitchDegrees * Mathf.Deg2Rad;
            float cosPitch = Mathf.Cos(pitch);
            return new Vector3(
                Mathf.Sin(yaw) * cosPitch,
                Mathf.Sin(pitch),
                Mathf.Cos(yaw) * cosPitch);
        }
    }
}
