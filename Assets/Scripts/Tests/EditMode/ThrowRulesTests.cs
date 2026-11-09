using NUnit.Framework;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Throwing (humanoid animation pass Phase 3): weight-to-range bands, the out-of-range clamp, a launch
    /// that lands on its target, and the throw clip table the server times its release from.
    /// </summary>
    public class ThrowRulesTests
    {
        [TestCase(0.1f, 10f)]
        [TestCase(0.5f, 10f)]
        [TestCase(1.0f, 9.22f)]
        [TestCase(2.75f, 6.5f)]
        [TestCase(5.0f, 3f)]
        [TestCase(6.0f, 3f)]
        public void Range_FallsWithWeight_FromLightToHeavy(float weight, float expected)
        {
            Assert.AreEqual(expected, ThrowRules.MaxRange(weight), 0.01f);
        }

        [Test]
        public void AboveTheWeightLimit_NothingIsThrown()
        {
            Assert.IsTrue(ThrowRules.IsThrowableWeight(ThrowRules.MaxThrowableWeight));
            Assert.IsFalse(ThrowRules.IsThrowableWeight(ThrowRules.MaxThrowableWeight + 0.1f));
            Assert.AreEqual(0f, ThrowRules.MaxRange(12f));
        }

        [Test]
        public void TargetInRange_IsKept()
        {
            Vector3 feet = new(1f, 0f, 2f);
            Vector3 target = new(4f, 0.8f, 6f);
            Vector3 landing = ThrowRules.ClampToRange(feet, target, 10f, out bool clamped);
            Assert.IsFalse(clamped);
            Assert.AreEqual(target, landing);
        }

        [Test]
        public void TargetBeyondRange_LandsAtMaxRange_OnTheSameHeading_AtFeetHeight()
        {
            Vector3 feet = new(0f, 0f, 0f);
            Vector3 target = new(30f, 1.5f, 40f);
            Vector3 landing = ThrowRules.ClampToRange(feet, target, 5f, out bool clamped);
            Assert.IsTrue(clamped);
            Assert.AreEqual(3f, landing.x, 0.001f);
            Assert.AreEqual(4f, landing.z, 0.001f);
            Assert.AreEqual(0f, landing.y, 0.001f);
        }

        [TestCase(1f, 0f)]
        [TestCase(6f, -0.6f)]
        [TestCase(10f, 1.2f)]
        public void LaunchVelocity_ReachesTheTargetPoint(float distance, float heightChange)
        {
            Vector3 origin = new(0f, 1.6f, 0f);
            Vector3 target = origin + new Vector3(distance * 0.6f, heightChange - 1.6f, distance * 0.8f);
            float flight = ThrowRules.FlightSeconds(origin, target);
            Vector3 velocity = ThrowRules.LaunchVelocity(origin, target, Physics.gravity, flight);
            Vector3 arrived = origin + velocity * flight + 0.5f * flight * flight * Physics.gravity;
            Assert.That(Vector3.Distance(arrived, target), Is.LessThan(0.001f));
            Assert.That(flight, Is.InRange(ThrowRules.MinFlightSeconds, ThrowRules.MaxFlightSeconds));
        }

        [Test]
        public void ReleasePoint_IsInFrontAtShoulderHeight_OnTheThrowingHandsSide()
        {
            Vector3 right = ThrowRules.ReleasePoint(Vector3.zero, Vector3.forward, leftHand: false);
            Vector3 left = ThrowRules.ReleasePoint(Vector3.zero, Vector3.forward, leftHand: true);
            Assert.AreEqual(ThrowRules.ReleaseHeight, right.y, 0.001f);
            Assert.Greater(right.z, 0f);
            Assert.Greater(right.x, 0f);
            Assert.Less(left.x, 0f);
        }

        [Test]
        public void ThrowClips_FitTheTwoBitVariant_AndReleaseInsideTheClip()
        {
            Assert.That(ThrowAnimation.Variants.Length, Is.InRange(1, 4));
            foreach (ThrowAnimation.Clip clip in ThrowAnimation.Variants)
            {
                Assert.Less(clip.FirstFrame, clip.ReleaseFrame, clip.ClipName);
                Assert.Less(clip.ReleaseFrame, clip.LastFrame, clip.ClipName);
                Assert.Less(clip.ReleaseSeconds, 0.8f, $"{clip.ClipName}: a throw that takes longer to leave the hand reads as lag");
                Assert.Less(clip.ReleaseSeconds, clip.LengthSeconds * ThrowAnimation.ExitTime,
                    $"{clip.ClipName}: the item leaves before the arm blends back to its hold pose");
            }
        }

        /// <summary>
        /// The armed-interaction and throw-mode overlays share this sheet. A stale local file id once left it
        /// null at runtime, which dropped every overlay element into flow layout (the chip stuck to the left).
        /// </summary>
        [Test]
        public void OverlayStyleSheet_ResolvesOnTheArmedInteractionPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Content/Systems/UI/Interactions/ArmedInteractionOverlay/ArmedInteractionOverlay.prefab");
            Assert.IsNotNull(prefab);
            SS3D.Systems.Interactions.ArmedInteractionSubSystem armed =
                prefab.GetComponent<SS3D.Systems.Interactions.ArmedInteractionSubSystem>();
            Assert.IsNotNull(armed.OverlayStyleSheet, "The overlay stylesheet reference resolves");
        }

        [Test]
        public void ThrowClips_ImportAsTrimmedHumanoidClips()
        {
            foreach (ThrowAnimation.Clip throwClip in ThrowAnimation.Variants)
            {
                AnimationClip clip = null;
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/Animations/{throwClip.File}"))
                {
                    if (asset is AnimationClip candidate && candidate.name == throwClip.ClipName)
                    {
                        clip = candidate;
                    }
                }

                Assert.IsNotNull(clip, $"{throwClip.ClipName} missing from {throwClip.File}");
                Assert.IsTrue(clip.humanMotion, throwClip.ClipName);
                Assert.AreEqual(throwClip.LengthSeconds, clip.length, 0.02f, throwClip.ClipName);
            }
        }
    }
}
