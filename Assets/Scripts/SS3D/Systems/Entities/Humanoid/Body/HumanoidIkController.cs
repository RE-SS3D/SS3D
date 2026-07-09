using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// IK hooks for combat look-at and future foot placement (#937).
    /// Phase 3 polish — scaffolded with capability-driven activation.
    /// </summary>
    public class HumanoidIkController : MonoBehaviour
    {
        [SerializeField] private HumanoidRigReferences _rig;
        [SerializeField] private Transform _lookAtTarget;
        [SerializeField] private float _headLookWeight = 0.6f;
        [SerializeField] private float _torsoLookWeight = 0.3f;
        [SerializeField] private float _blockedStandUpHeadroom = 1.2f;

        private bool _combatLookActive;
        private float _aimYaw;
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_rig == null)
            {
                _rig = GetComponent<HumanoidRigReferences>();
            }
        }

        public void SetCombatLookAt(bool active, float aimYaw)
        {
            _combatLookActive = active;
            _aimYaw = aimYaw;
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
            if (_animator == null || _rig == null || _rig.Head == null)
            {
                return;
            }

            if (!_combatLookActive)
            {
                _animator.SetLookAtWeight(0f);
                return;
            }

            Vector3 lookDirection = Quaternion.Euler(0f, _aimYaw, 0f) * transform.forward;
            Vector3 lookTarget = _rig.Head.position + lookDirection * 2f;

            if (_lookAtTarget != null)
            {
                _lookAtTarget.position = lookTarget;
            }

            _animator.SetLookAtWeight(_headLookWeight, _torsoLookWeight);
            _animator.SetLookAtPosition(lookTarget);
        }
    }
}
