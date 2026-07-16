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
        [SerializeField] private float _blockedStandUpHeadroom = 1.2f;

        private bool _combatLookActive;
        private float _aimYaw;
        private float _aimPitch;
        private bool _hasWorldAimPoint;
        private Vector3 _worldAimPoint;
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

            Vector3 lookTarget = _hasWorldAimPoint
                ? _worldAimPoint
                : head.position + AimDirection(_aimYaw, _aimPitch) * _lookDistance;

            if (_lookAtTarget != null)
            {
                _lookAtTarget.position = lookTarget;
            }

            _animator.SetLookAtWeight(_headLookWeight, _torsoLookWeight);
            _animator.SetLookAtPosition(lookTarget);
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
