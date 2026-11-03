using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// The rifle reload follows its animation: clip, gameplay timer and animator exit must agree.
    /// </summary>
    public class RifleReloadClipTimingTests
    {
        private const string ControllerPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanCharacterAnimator.controller";
        private const string M4Path = "Assets/Content/WorldObjects/Items/Weapons/M4.prefab";

        [Test]
        public void ClipLength_MatchesMeasuredTimings()
        {
            AnimatorState state = FindReloadState();
            AnimationClip clip = state.motion as AnimationClip;

            Assert.IsNotNull(clip, "Reload Rifle state has no clip");
            Assert.AreEqual(RifleReloadClipTiming.ClipSeconds, clip.length, 0.02f,
                "Reload clip changed: re-measure RifleReloadClipTiming");
            Assert.AreEqual(1f, state.speed, 1e-5f, "timings assume the clip plays at speed 1");
        }

        [Test]
        public void AnimatorExit_LandsWhenReloadCompletes()
        {
            AnimatorStateTransition exit = FindReloadState().transitions.Single(t => t.hasExitTime);

            Assert.AreEqual(RifleReloadClipTiming.Normalized(RifleReloadClipTiming.ReadySeconds), exit.exitTime, 0.005f);
        }

        [Test]
        public void M4ReloadSeconds_MatchesClipReadyMoment()
        {
            RangedWeaponItemExtension weapon = AssetDatabase.LoadAssetAtPath<GameObject>(M4Path)
                .GetComponent<RangedWeaponItemExtension>();

            Assert.AreEqual(RifleReloadClipTiming.ReadySeconds, weapon.Profile.ReloadSeconds, 1e-4f);
            Assert.AreEqual(RifleReloadClipTiming.ReadySeconds, RangedWeaponProfile.M4.ReloadSeconds, 1e-4f);
        }

        [Test]
        public void SoundCues_FallInsideTheReload()
        {
            Assert.Less(RifleReloadClipTiming.MagazineInSeconds, RifleReloadClipTiming.BoltReleaseSeconds);
            Assert.Less(RifleReloadClipTiming.BoltReleaseSeconds, RifleReloadClipTiming.ReadySeconds);
        }

        private static AnimatorState FindReloadState()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.IsNotNull(controller, ControllerPath);
            // Upper Body plays the reload; Base Layer keeps a muted legacy copy of the state.
            AnimatorState state = controller.layers
                .Where(layer => layer.name == "Upper Body")
                .SelectMany(layer => layer.stateMachine.states)
                .Select(child => child.state)
                .FirstOrDefault(s => s.name == "Reload Rifle");
            Assert.IsNotNull(state, "Upper Body Reload Rifle state missing");
            return state;
        }
    }
}
