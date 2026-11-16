using SS3D.Systems.Entities.Data;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// Downed presentation (health.md §4), on the Full Body Override layer (<see cref="DownedAnimation"/>). A standing
    /// body plays the fall for its gait; the clip's travel moves the controller, so walls stop it. At the end the bones
    /// blend into the lying state for the way the body faces: belly down crawls, face up lies still. A face-up body
    /// rolls onto its belly the first time it crawls. A body already on the floor (Critical, a knockdown) blends
    /// straight in. Leaving Downed stands up with the get-up clip for its facing.
    /// </summary>
    public partial class Ragdoll
    {
        /// <summary>The running bone blend ends in a Downed state rather than a get-up.</summary>
        private bool _blendIntoDowned;
        /// <summary>The Downed state the running blend ends in.</summary>
        private string _downedTarget = DownedAnimation.CrawlState;
        /// <summary>The animator holds the body in a lying state; leaving Downed has to stand up from it.</summary>
        private bool _inDownedPose;
        private bool _downedFaceUp;
        /// <summary>A fall clip is playing: its root motion moves the body and its end starts the blend.</summary>
        private bool _downedFalling;
        private int _downedFallHash;
        private int _downedFallFrame;

        /// <summary>Downed, but still falling or blending into the lying pose: hands and movement wait for it.</summary>
        public bool IsFallingDowned => IsDowned && (_currentState != RagdollState.Walking || _downedFalling);

        private void EnterDowned(bool snap)
        {
            switch (_currentState)
            {
                case RagdollState.Walking:
                    if (_inDownedPose || _downedFalling)
                    {
                        return;
                    }

                    if (snap)
                    {
                        PlayDownedPose(DownedAnimation.CrawlState);
                        return;
                    }

                    StartDownedFall();
                    return;
                case RagdollState.Ragdoll:
                    // Critical or a knockdown already has the body on the floor.
                    BonesReset(intoDowned: true);
                    return;
                default:
                    if (_currentState == RagdollState.BonesReset && _blendIntoDowned)
                    {
                        return;
                    }

                    // Mid get-up: go back down from the current pose.
                    FreezePose();
                    BonesReset(intoDowned: true);
                    return;
            }
        }

        private void StartDownedFall()
        {
            EnsureAnimatorCached();
            DownedAnimation.Clip fall = DownedAnimation.FallFor(_animator.GetFloat(Animations.Humanoid.MovementSpeed));
            _downedFalling = true;
            _downedFallHash = Animator.StringToHash(fall.StateName);
            _downedFallFrame = Time.frameCount;
            // Input stops; the controller stays on for the owner so the fall's root motion collides.
            if (_humanoidLivingController != null)
            {
                _humanoidLivingController.enabled = false;
            }

            SetDownedLayerWeight(1f);
            _animator.CrossFadeInFixedTime(fall.StateName, 0.15f, DownedAnimation.Layer, 0f);
        }

        /// <summary>Walking-state tick: end a finished fall, or roll a face-up body that starts to crawl.</summary>
        private void TickDownedPose()
        {
            if (_downedFalling)
            {
                AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(DownedAnimation.Layer);
                if (Time.frameCount > _downedFallFrame + 1 && !_animator.IsInTransition(DownedAnimation.Layer)
                    && (state.shortNameHash != _downedFallHash || state.normalizedTime >= 1f))
                {
                    _downedFalling = false;
                    FreezePose();
                    BonesReset(intoDowned: true);
                }

                return;
            }

            if (_inDownedPose && _downedFaceUp && WantsToCrawl())
            {
                FreezePose();
                BonesReset(intoDowned: true);
            }
        }

        private bool WantsToCrawl() =>
            (_humanoidLivingController != null && _humanoidLivingController.enabled
                && _humanoidLivingController.TargetMovement.sqrMagnitude > 0.01f)
            || _animator.GetFloat(Animations.Humanoid.MovementSpeed) > 0.05f;

        /// <summary>Lying state the blend ends in: a face-up body stays on its back unless it is rolling over to crawl.</summary>
        private string ChooseDownedTarget()
        {
            bool rollToBelly = _inDownedPose && _downedFaceUp;
            _downedTarget = rollToBelly || IsHipsFacingDown() ? DownedAnimation.CrawlState : DownedAnimation.FaceUpState;
            _inDownedPose = false;
            return _downedTarget;
        }

        /// <summary>Move the body by the fall clip's travel. Only the physics authority moves it; others follow its root.</summary>
        private void OnAnimatorMove()
        {
            if (!_downedFalling || _animator == null || !IsRagdollPhysicsAuthority())
            {
                return;
            }

            Vector3 delta = _animator.deltaPosition;
            delta.y = -2f * Time.deltaTime; // a body that falls off a ledge still lands
            if (_characterController != null && _characterController.enabled)
            {
                _characterController.Move(delta);
            }
            else
            {
                transform.position += new Vector3(delta.x, 0f, delta.z);
            }
        }

        private void PlayDownedPose(string stateName)
        {
            _blendIntoDowned = false;
            _currentState = RagdollState.Walking;
            _inDownedPose = true;
            _downedFaceUp = stateName == DownedAnimation.FaceUpState;
            ToggleKinematic(true);
            ToggleSyncRagdoll(false);
            ToggleAnimator(true);
            SetDownedLayerWeight(1f);
            _animator.Play(stateName, DownedAnimation.Layer, 0f);
            ToggleController(true);
            EnableAnimationDrivers();
        }

        /// <summary>Freeze the lying (or falling) pose and blend it into the get-up clip, as a ragdoll recovery does.</summary>
        private void StandUpFromDownedPose()
        {
            _inDownedPose = false;
            _downedFalling = false;
            FreezePose();
            BonesReset();
        }

        /// <summary>Hold the bones where the animator left them, with input and drivers off, ready for a bone blend.</summary>
        private void FreezePose()
        {
            ToggleController(false);
            DisableAnimationDrivers();
            ToggleAnimator(false);
        }

        private void SetDownedLayerWeight(float weight)
        {
            if (_animator != null && _animator.layerCount > DownedAnimation.Layer)
            {
                _animator.SetLayerWeight(DownedAnimation.Layer, weight);
            }
        }
    }
}
