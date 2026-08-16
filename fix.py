using UnityEngine;
using UnityEngine.Animations;

namespace SS3D
{
    public class CharacterAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _animSpeed = 1f;

        private enum Locomotion { Idle = 0, Walk = 1, Run = 2, Sprint = 3, Crawl = 4, Emote = 5 }
        private enum BodyPose { Stand = 0, Sit = 1, CrawlPos = 2, Ragdoll = 3 }
        private enum ArmHold { Hold1 = 0, Hold2 = 1, Throw = 2, Hit = 3 }
        private enum ArmInjury { Healthy = 0, Injured = 1, Broken = 2 }

        private Locomotion _loco;
        private BodyPose _body;
        private ArmHold _arm;
        private ArmInjury _injury;
        private bool _isRagdoll;

        public void Start()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            _animator.SetInteger("ArmHold", (int)_arm);
            _animator.SetInteger("ArmInjury", (int)_injury);
            _animator.SetFloat("Locomotion", (float)_loco);
            _animator.SetFloat("BodyPose", (float)_body);
        }

        public void Update()
        {
            if (_animator != null && !_isRagdoll)
            {
                _animator.SetFloat("Locomotion", (float)_loco, _animSpeed, Time.deltaTime);
                _animator.SetFloat("BodyPose", (float)_body, _animSpeed, Time.deltaTime);
                _animator.SetInteger("ArmHold", (int)_arm);
                _animator.SetInteger("ArmInjury", (int)_injury);
            }
        }

        public void SetLocomotionState(Locomotion state)
        {
            _loco = state;
            _animator.SetFloat("Locomotion", (float)_loco);
        }

        public void SetBodyPoseState(BodyPose state)
        {
            _body = state;
            _animator.SetFloat("BodyPose", (float)_body);
        }

        public void SetArmHold(ArmHold state)
        {
            _arm = state;
            _animator.SetInteger("ArmHold", (int)_arm);
        }

        public void SetArmInjury(ArmInjury state)
        {
            _injury = state;
            _animator.SetInteger("ArmInjury", (int)_injury);
        }

        public void SetRagdoll(bool state)
        {
            _isRagdoll = state;
            if (state) _animator.SetInteger("BodyPose", (int)BodyPose.Ragdoll);
        }

        public void SetLocomotion(float value) => _animator.SetFloat("Locomotion", value);
        public void SetBodyPose(float value) => _animator.SetFloat("BodyPose", value);

        public void TriggerThrow() => SetArmHold(ArmHold.Throw);
        public void TriggerHit() => SetArmHold(ArmHold.Hit);
        public void SetLeftHold() => SetArmHold(ArmHold.Hold1);
        public void SetRightHold() => SetArmHold(ArmHold.Hold2);

        public Locomotion GetLocomotion() => _loco;
        public BodyPose GetBodyPose() => _body;
    }
}