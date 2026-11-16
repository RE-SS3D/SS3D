using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    public class GetUpAnimationTests
    {
        private const string HumanPrefabPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
        private const string ControllerPath =
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanCharacterAnimator.controller";

        [Test]
        public void Clips_ImportAsTrimmedHumanoidClips()
        {
            foreach (GetUpAnimation.Clip getUp in GetUpAnimation.All)
            {
                AnimationClip clip = LoadClip(getUp);
                Assert.IsNotNull(clip, $"{getUp.ClipName} missing from {getUp.File}");
                Assert.IsTrue(clip.humanMotion, getUp.ClipName);
                Assert.IsFalse(clip.isLooping, getUp.ClipName);
                Assert.AreEqual(getUp.LengthSeconds, clip.length, 0.02f, getUp.ClipName);
            }
        }

        [Test]
        public void PlaybackRate_MeetsTheTargetUnlessThatWouldPassTheRateLimit()
        {
            Assert.AreEqual(1f, GetUpAnimation.ComputePlaybackRate(0.8f), "A short clip plays at natural speed");
            Assert.AreEqual(2f, GetUpAnimation.ComputePlaybackRate(GetUpAnimation.TargetSeconds * 2f), 1e-4f);
            Assert.AreEqual(GetUpAnimation.MaxPlaybackRate, GetUpAnimation.ComputePlaybackRate(60f));
            foreach (GetUpAnimation.Clip getUp in GetUpAnimation.All)
            {
                bool reachesTarget = getUp.DurationSeconds <= GetUpAnimation.TargetSeconds + 1e-4f;
                bool rateLimited = Mathf.Approximately(getUp.PlaybackRate, GetUpAnimation.MaxPlaybackRate);
                Assert.IsTrue(reachesTarget || rateLimited,
                    $"{getUp.ClipName} plays in {getUp.DurationSeconds:F2}s at {getUp.PlaybackRate:F2}×");
            }
        }

        [Test]
        public void States_PlayTheTableClipsAtTheTableRate_AndBlendOutIntoEveryStance()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimatorStateMachine baseMachine = controller.layers[0].stateMachine;
            foreach (GetUpAnimation.Clip getUp in GetUpAnimation.All)
            {
                AnimatorState state = baseMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == getUp.StateName);
                Assert.IsNotNull(state, $"Base Layer has no {getUp.StateName} state");
                Assert.AreEqual(getUp.ClipName, state.motion != null ? state.motion.name : null, getUp.StateName);
                Assert.AreEqual(getUp.PlaybackRate, state.speed, 1e-4f, getUp.StateName);
                Assert.AreEqual(3, state.transitions.Length, $"{getUp.StateName} exits into peaceful, melee and ranged");
                foreach (AnimatorStateTransition exit in state.transitions)
                {
                    Assert.IsTrue(exit.hasExitTime, getUp.StateName);
                    Assert.AreEqual(GetUpAnimation.BlendOutStart, exit.exitTime, 1e-4f, getUp.StateName);
                    Assert.AreEqual(getUp.BlendOutSeconds, exit.duration, 1e-4f, getUp.StateName);
                }
            }
        }

        /// <summary>
        /// The old baked clips evaluated to the avatar's T-pose at every time, so the blend into "frame 0" stood
        /// the ragdoll up into a T-pose. The first frame must lie on the floor and the last must stand.
        /// </summary>
        [Test]
        public void States_StartLyingDown_AndEndStanding()
        {
            GameObject body = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath));
            try
            {
                Animator animator = body.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                foreach (GetUpAnimation.Clip getUp in GetUpAnimation.All)
                {
                    Assert.Less(HipsHeightAt(animator, hips, getUp.StateName, 0f), 0.3f, $"{getUp.StateName} starts lying down");
                    Assert.Greater(HipsHeightAt(animator, hips, getUp.StateName, 1f), 0.7f, $"{getUp.StateName} ends standing");
                }
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }

        private static float HipsHeightAt(Animator animator, Transform hips, string state, float normalizedTime)
        {
            animator.enabled = true;
            animator.Play(state, 0, normalizedTime);
            animator.Update(0f);
            animator.enabled = false;
            return hips.position.y - animator.transform.position.y;
        }

        private static AnimationClip LoadClip(GetUpAnimation.Clip getUp) =>
            AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/Animations/{getUp.File}")
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => clip.name == getUp.ClipName);
    }
}
