using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// IK hooks for combat look-at and future foot placement (#937).
    /// </summary>
    public class HumanoidIkController : MonoBehaviour
    {
        [SerializeField] private HumanoidRigReferences _rig;
        [SerializeField] private Transform _lookAtTarget;
        [SerializeField] private float _headLookWeight = 0.65f;
        [SerializeField] private float _torsoLookWeight = 0.45f;
        [SerializeField] private float _lookDistance = 4f;
        /// <summary>Matches combat body yaw so head look-at does not snap ahead of the torso.</summary>
        [SerializeField] private float _lookAtLerpMultiplier = 3.5f;
        [SerializeField] private float _meleeIkBlendLerp = 6f;
        [SerializeField] private float _blockedStandUpHeadroom = 1.2f;

        private bool _combatLookActive;
        private float _meleeAttackIkBlend;
        private float _meleeAttackIkBlendTarget;
        private float _aimYaw;
        private float _aimPitch;
        private bool _hasWorldAimPoint;
        private Vector3 _worldAimPoint;
        private Vector3 _smoothedLookTarget;
        private bool _hasSmoothedLookTarget;
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_rig == null)
            {
                _rig = GetComponent<HumanoidRigReferences>();
            }
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
                _meleeAttackIkBlendTarget = 0f;
            }
        }

        /// <summary>
        /// While a melee swing plays, torso look-at eases out; head keeps aiming at the target.
        /// Upper-body mask is arms-only so the swing clip cannot counter-rotate the head.
        /// </summary>
        public void SetMeleeAttackActive(bool active)
        {
            _meleeAttackIkBlendTarget = active ? 1f : 0f;
            if (active)
            {
                // Snappy suppress at swing start; release is smoothed in OnAnimatorIK.
                _meleeAttackIkBlend = 1f;
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
            return !Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.up, _blockedStandUpHeadroom);
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

            _meleeAttackIkBlend = Mathf.MoveTowards(
                _meleeAttackIkBlend,
                _meleeAttackIkBlendTarget,
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

            // SetLookAtWeight(global, body, head).
            // Swing clip is arms-only (head masked out) — keep head aiming at the target while
            // torso look-at eases out so Mixamo counter-rotation does not yank the head.
            float bodyWeight = Mathf.Lerp(_torsoLookWeight, 0f, _meleeAttackIkBlend);
            float headWeight = _headLookWeight;
            _animator.SetLookAtWeight(1f, bodyWeight, headWeight);
            _animator.SetLookAtPosition(_smoothedLookTarget);
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
