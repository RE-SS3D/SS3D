using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Interaction animations (animation pass Phase 4): the clip table, its imports and animator states, which
    /// interaction plays what, and the moving fallback.
    /// </summary>
    public class InteractionAnimationTests
    {
        private const string HumanPrefabPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
        private const string ControllerPath =
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanCharacterAnimator.controller";

        [Test]
        public void Clips_ImportAsTrimmedHumanoidClips()
        {
            foreach (InteractionAnimation.Clip entry in InteractionAnimation.All)
            {
                AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/Animations/{entry.File}")
                    .OfType<AnimationClip>()
                    .FirstOrDefault(c => c.name == entry.ClipName);
                Assert.IsNotNull(clip, $"{entry.ClipName} missing from {entry.File}");
                Assert.IsTrue(clip.humanMotion, entry.ClipName);
                Assert.AreEqual(entry.Loop, clip.isLooping, entry.ClipName);
                Assert.AreEqual((entry.LastFrame - entry.FirstFrame) / InteractionAnimation.ClipFps, clip.length, 0.02f, entry.ClipName);
            }
        }

        [Test]
        public void ContactFrames_WaitAtMostTheBudget_AndDoorsStayInstant()
        {
            foreach (int waits in new[]
                     {
                         InteractionAnimation.Reach, InteractionAnimation.LockerOpen, InteractionAnimation.LockerClose,
                         InteractionAnimation.LockerTap, InteractionAnimation.FloorPickup, InteractionAnimation.FloorPutDown,
                         InteractionAnimation.LightSwitch,
                     })
            {
                InteractionAnimation.Clip clip = InteractionAnimation.All[waits];
                Assert.IsTrue(clip.WaitsForContact, $"{clip.StateName} waits for the hand");
                Assert.That(clip.ContactFrame, Is.InRange(clip.FirstFrame + 1, clip.LastFrame), clip.StateName);
                Assert.LessOrEqual(clip.ContactSeconds, InteractionAnimation.MaxContactSeconds + 1e-4f, clip.StateName);
            }

            Assert.AreEqual(0f, InteractionAnimation.ContactSeconds(InteractionAnimation.DoorReach), "Doors open at once");
            Assert.AreEqual(0f, InteractionAnimation.ContactSeconds(InteractionAnimation.Kneel), "The kneel rides a delayed interaction");
            Assert.AreEqual(0f, InteractionAnimation.ContactSeconds(InteractionAnimation.None));
        }

        [TestCase("Pickup", 0.1f, false, InteractionAnimation.FloorPickup)]
        [TestCase("Pickup", 0.85f, false, InteractionAnimation.Reach)]
        [TestCase("Drop", 0.02f, false, InteractionAnimation.FloorPutDown)]
        [TestCase("Drop", 0.9f, false, InteractionAnimation.Reach)]
        [TestCase("OpenLocker", 1f, false, InteractionAnimation.LockerOpen)]
        [TestCase("OpenLocker", 1f, true, InteractionAnimation.LockerClose)]
        [TestCase("LockLocker", 1f, false, InteractionAnimation.LockerTap)]
        [TestCase("UnlockLocker", 1f, false, InteractionAnimation.LockerTap)]
        [TestCase("OpenAirlock", 1f, false, InteractionAnimation.DoorReach)]
        [TestCase(InteractionAnimation.LightSwitchName, 1f, false, InteractionAnimation.LightSwitch)]
        [TestCase("Toggle", 1f, false, InteractionAnimation.None)]
        [TestCase("AttachCharge", 1f, false, InteractionAnimation.DoorReach)]
        [TestCase("DefuseC4", 0f, false, InteractionAnimation.Kneel)]
        [TestCase("Perform CPR", 0f, false, InteractionAnimation.Kneel)]
        [TestCase("ArmC4", 1f, false, InteractionAnimation.None)]
        [TestCase("Bandage", 1f, false, InteractionAnimation.None)]
        public void ChooseClip_MatchesThePlaytestTier(string genericName, float height, bool closesLocker, int expected)
        {
            Assert.AreEqual(expected, InteractionAnimation.ChooseClip(genericName, height, closesLocker));
        }

        [Test]
        public void ResolvePlayback_FallsBackToTheReach_WhenAFloorBendCannotPlay()
        {
            Assert.AreEqual(InteractionAnimation.FloorPickup,
                InteractionAnimation.ResolvePlayback(InteractionAnimation.FloorPickup, moving: false, standing: true));
            Assert.AreEqual(InteractionAnimation.Reach,
                InteractionAnimation.ResolvePlayback(InteractionAnimation.FloorPickup, moving: true, standing: true),
                "Moving, the bend becomes the standing reach (Decision 22)");
            Assert.AreEqual(InteractionAnimation.Reach,
                InteractionAnimation.ResolvePlayback(InteractionAnimation.FloorPutDown, moving: false, standing: false),
                "Seated or floating, the bend becomes the reach");
            Assert.AreEqual(InteractionAnimation.DoorReach,
                InteractionAnimation.ResolvePlayback(InteractionAnimation.DoorReach, moving: true, standing: true),
                "Reaches play over locomotion");
            Assert.AreEqual(InteractionAnimation.None,
                InteractionAnimation.ResolvePlayback(InteractionAnimation.Kneel, moving: false, standing: true),
                "The kneel is replicated state, not a oneshot");
            Assert.AreEqual(InteractionAnimation.None,
                InteractionAnimation.ResolvePlayback(InteractionAnimation.None, moving: false, standing: true));
        }

        [Test]
        public void States_SitOnTheirLayer_EnterOnTheirClipIndex_AndMirror()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            for (int index = 0; index < InteractionAnimation.All.Length; index++)
            {
                InteractionAnimation.Clip entry = InteractionAnimation.All[index];
                AnimatorStateMachine machine = controller.layers[entry.FullBody ? 0 : 1].stateMachine;
                AnimatorState state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == entry.StateName);
                Assert.IsNotNull(state, $"{(entry.FullBody ? "Base Layer" : "Upper Body")} has no {entry.StateName} state");
                Assert.AreEqual(entry.ClipName, state.motion != null ? state.motion.name : null, entry.StateName);
                Assert.AreEqual(entry.PlaybackRate, state.speed, 1e-4f, entry.StateName);

                AnimatorStateTransition[] entries = machine.anyStateTransitions.Where(t => t.destinationState == state).ToArray();
                Assert.AreEqual(1, entries.Length, $"{entry.StateName} has one way in");
                if (entry.Loop)
                {
                    Assert.IsTrue(entries[0].conditions.Any(c => c.parameter == InteractionAnimation.KneelParameter
                        && c.mode == AnimatorConditionMode.If), entry.StateName);
                    Assert.IsTrue(state.transitions.All(t => t.conditions.Any(c => c.parameter == InteractionAnimation.KneelParameter
                        && c.mode == AnimatorConditionMode.IfNot)), $"{entry.StateName} leaves when the kneel clears");
                    continue;
                }

                Assert.IsTrue(state.mirrorParameterActive, $"{entry.StateName} mirrors for the left hand");
                Assert.IsTrue(entries[0].canTransitionToSelf, $"{entry.StateName} restarts on a new interaction");
                Assert.IsTrue(entries[0].conditions.Any(c => c.parameter == InteractionAnimation.TriggerParameter), entry.StateName);
                Assert.IsTrue(entries[0].conditions.Any(c => c.parameter == InteractionAnimation.ClipParameter
                    && (int)c.threshold == index), $"{entry.StateName} enters on InteractClip {index}");
                if (entry.FullBody)
                {
                    Assert.IsTrue(state.transitions.Any(t => !t.hasExitTime && t.conditions.Any(c => c.parameter == "Speed")),
                        $"{entry.StateName} leaves as soon as the body moves");
                }

                if (entry.HasReturn)
                {
                    AnimatorState back = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == entry.ReturnStateName);
                    Assert.IsNotNull(back, $"Upper Body has no {entry.ReturnStateName} state");
                    Assert.AreEqual(entry.ReturnClipName, back.motion != null ? back.motion.name : null, entry.ReturnStateName);
                    Assert.AreEqual(1, state.transitions.Length, $"{entry.StateName} leaves only into its return");
                    Assert.AreSame(back, state.transitions[0].destinationState, entry.StateName);
                    Assert.IsTrue(back.mirrorParameterActive, $"{entry.ReturnStateName} mirrors for the left hand");
                    Assert.AreEqual(3, back.transitions.Length, $"{entry.ReturnStateName} leaves for the hold poses");
                }
            }
        }

        /// <summary>The trimmed clips must show the motion the table names: the kneel low, the bend's hand at the floor.</summary>
        [Test]
        public void FullBodyStates_ReachTheFloor()
        {
            GameObject body = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath));
            try
            {
                Animator animator = body.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                float standingHips = HeightAt(animator, hips, "Peaceful Locomotion", 0f);

                InteractionAnimation.Clip kneel = InteractionAnimation.All[InteractionAnimation.Kneel];
                Assert.Less(HeightAt(animator, hips, kneel.StateName, 0.5f), standingHips * 0.7f, "The kneel is down on one knee");

                foreach (int bend in new[] { InteractionAnimation.FloorPickup, InteractionAnimation.FloorPutDown })
                {
                    InteractionAnimation.Clip entry = InteractionAnimation.All[bend];
                    float lowest = Enumerable.Range(0, 11).Min(i => HeightAt(animator, hand, entry.StateName, i / 10f));
                    Assert.Less(lowest, 0.4f, $"{entry.StateName} brings the hand near the floor");
                }
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }

        private static float HeightAt(Animator animator, Transform bone, string state, float normalizedTime)
        {
            animator.enabled = true;
            animator.Play(state, 0, normalizedTime);
            animator.Update(0f);
            animator.enabled = false;
            return bone.position.y - animator.transform.position.y;
        }
    }
}
