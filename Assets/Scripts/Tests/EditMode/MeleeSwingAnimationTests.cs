using NUnit.Framework;
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEditor;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    public class MeleeSwingAnimationTests
    {
        [Test]
        public void Variants_FitTheTwoBitSnapshotField_AndStrikeInsideTheClip()
        {
            Assert.That(MeleeSwingAnimation.Variants.Length, Is.InRange(1, 4));
            foreach (MeleeSwingAnimation.Swing swing in MeleeSwingAnimation.Variants)
            {
                Assert.Less(swing.FirstFrame, swing.StrikeFrame, swing.ClipName);
                Assert.Less(swing.StrikeFrame, swing.LastFrame, swing.ClipName);
            }
        }

        [Test]
        public void Variants_ImportAsTrimmedHumanoidClips()
        {
            foreach (MeleeSwingAnimation.Swing swing in MeleeSwingAnimation.Variants)
            {
                AnimationClip clip = null;
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/Animations/{swing.File}"))
                {
                    if (asset is AnimationClip candidate && candidate.name == swing.ClipName)
                    {
                        clip = candidate;
                    }
                }

                Assert.IsNotNull(clip, $"{swing.ClipName} missing from {swing.File}");
                Assert.IsTrue(clip.humanMotion, swing.ClipName);
                Assert.AreEqual(swing.LengthSeconds, clip.length, 0.02f, swing.ClipName);
            }
        }

        [Test]
        public void WindupRate_LandsStrikeAtTheProfileWindup_ForEveryShippedWeapon()
        {
            MeleeWeaponProfile[] profiles =
            {
                MeleeWeaponProfile.Fists, MeleeWeaponProfile.Improvised, MeleeWeaponProfile.Crowbar,
                MeleeWeaponProfile.Hatchet, MeleeWeaponProfile.KitchenKnife,
            };

            foreach (MeleeWeaponProfile profile in profiles)
            {
                foreach (float exertion in new[] { 0f, 1f })
                {
                    float windup = profile.WindupSeconds * MeleeHitInteraction.ComputeExertionTimeMultiplier(exertion);
                    float quantized = MeleeSwingAnimation.DequantizeWindup(MeleeSwingAnimation.QuantizeWindup(windup));
                    for (int variant = 0; variant < MeleeSwingAnimation.Variants.Length; variant++)
                    {
                        float rate = MeleeSwingAnimation.ComputeWindupRate(variant, quantized);
                        float strikeAt = MeleeSwingAnimation.Variants[variant].StrikeSeconds / rate;
                        Assert.AreEqual(windup, strikeAt, 0.02f,
                            $"variant {variant} windup {windup:F2}s strikes at {strikeAt:F2}s (rate {rate:F2} clamped?)");
                    }
                }
            }
        }

        [Test]
        public void WindupRate_IsNaturalSpeed_WhenWindupUnknown()
        {
            Assert.AreEqual(1f, MeleeSwingAnimation.ComputeWindupRate(0, 0f));
            Assert.AreEqual(1f, MeleeSwingAnimation.ComputeWindupRate(99, 0.3f));
        }

        [Test]
        public void Windup_QuantizesInto5Bits_AndNeverRoundsAPositiveWindupToUnknown()
        {
            Assert.AreEqual(0, MeleeSwingAnimation.QuantizeWindup(0f));
            Assert.AreEqual(1, MeleeSwingAnimation.QuantizeWindup(0.001f));
            Assert.AreEqual(MeleeSwingAnimation.WindupQuantizedMax, MeleeSwingAnimation.QuantizeWindup(5f));
            Assert.AreEqual(0.3f, MeleeSwingAnimation.DequantizeWindup(MeleeSwingAnimation.QuantizeWindup(0.3f)), 1e-5f);
        }
    }
}
