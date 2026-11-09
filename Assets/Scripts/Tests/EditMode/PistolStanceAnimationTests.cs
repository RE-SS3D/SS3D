using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities.Humanoid.Body;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>Pistol hold (humanoid animation pass, Phase 2): data, animator wiring and the posed result.</summary>
    public class PistolStanceAnimationTests
    {
        private const string HumanPrefabPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
        private const string ControllerPath =
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanCharacterAnimator.controller";

        /// <summary>Two hands on one grip are a few cm apart; a hand at the hip is tens of cm from the gun.</summary>
        private const float GripDistance = 0.12f;

        [Test]
        public void HoldStyle_IsWeaponData_NotInferredFromTheTwoHandRule()
        {
            Assert.AreEqual(WeaponHoldStyle.Pistol, RangedWeaponProfile.Taser.HoldStyle);
            Assert.IsFalse(RangedWeaponProfile.Taser.RequiresBothHands);
            Assert.AreEqual(WeaponHoldStyle.Rifle, RangedWeaponProfile.M4.HoldStyle);
            Assert.AreEqual(WeaponHoldStyle.Rifle, RangedWeaponProfile.LaserRifle.HoldStyle);
            Assert.AreEqual(WeaponHoldStyle.Rifle, RangedWeaponProfile.IonRifle.HoldStyle);
        }

        [Test]
        public void Snapshot_RoundTripsPistolHoldAndOffHand_WithoutDisturbingWindup()
        {
            BodyAnimationSnapshot snapshot = BodyAnimationSnapshot.Default;
            snapshot.AttackWindup = 31;
            snapshot.PistolHold = true;
            snapshot.OffHandBusy = true;
            snapshot.MirrorUpperBody = true;

            BodyAnimationSnapshot unpacked = BodyAnimationSnapshot.Unpack(snapshot.Pack(), 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            Assert.IsTrue(unpacked.PistolHold);
            Assert.IsTrue(unpacked.OffHandBusy);
            Assert.IsTrue(unpacked.MirrorUpperBody);
            Assert.AreEqual(31, unpacked.AttackWindup);
            Assert.IsFalse(BodyAnimationSnapshot.Unpack(BodyAnimationSnapshot.Default.Pack(), 0f, 0f, 0f, 0f, 0f, 0f, 0f).PistolHold);
        }

        [Test]
        public void ShotKick_RisesToItsPeak_ThenSettlesToZero()
        {
            Assert.AreEqual(0f, PistolStanceAnimation.EvaluateShotKick(-0.01f));
            Assert.AreEqual(PistolStanceAnimation.ShotKickDegrees,
                PistolStanceAnimation.EvaluateShotKick(PistolStanceAnimation.ShotKickRiseSeconds), 1e-4f);
            Assert.Less(PistolStanceAnimation.EvaluateShotKick(0.15f), PistolStanceAnimation.EvaluateShotKick(0.06f));
            Assert.Less(PistolStanceAnimation.EvaluateShotKick(PistolStanceAnimation.ShotKickTotalSeconds - 1e-3f), 0.1f);
            Assert.AreEqual(0f, PistolStanceAnimation.EvaluateShotKick(PistolStanceAnimation.ShotKickTotalSeconds));
        }

        [Test]
        public void Clips_ImportAsLoopingHumanoidClips()
        {
            foreach (PistolStanceAnimation.Clip entry in PistolStanceAnimation.Locomotion)
            {
                AnimationClip clip = LoadClip(entry);
                Assert.IsNotNull(clip, $"{entry.ClipName} missing from {entry.File}");
                Assert.IsTrue(clip.humanMotion, entry.ClipName);
                Assert.IsTrue(clip.isLooping, entry.ClipName);
            }
        }

        /// <summary>
        /// The body moves at one speed per ring whatever the direction, so every clip on a ring must cover
        /// the same ground per second after calibration, or that direction slides.
        /// </summary>
        [Test]
        public void PistolTree_CoversTheSameGroundInEveryDirectionOfARing()
        {
            BlendTree pistol = FindPistolTree(out BlendTree holdBlend);
            Assert.AreEqual(PistolStanceAnimation.HoldParameter, holdBlend.blendParameter);
            Assert.AreEqual(1f, holdBlend.children[1].threshold);

            foreach (float ring in new[] { 0.3f, 1f })
            {
                float[] speeds = pistol.children
                    .Where(c => Mathf.Abs(c.position.magnitude - ring) < 0.01f)
                    .Select(c => PlanarSpeed((AnimationClip)c.motion) * c.timeScale)
                    .ToArray();
                Assert.AreEqual(4, speeds.Length, $"ring {ring} has forward, back and both strafes");
                Assert.Less(speeds.Max() / speeds.Min(), 1.1f, $"ring {ring} speeds {string.Join(", ", speeds)}");
            }
        }

        /// <summary>
        /// With an empty off-hand the pack grips the gun with both hands. A full off-hand weights the Off Hand
        /// layer opposite the gun, which takes only that arm out of the grip; mirrored for a left-hand gun.
        /// </summary>
        [Test]
        public void OffHandLayer_TakesOnlyTheOtherArmOutOfTheGrip()
        {
            GameObject body = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath));
            try
            {
                Animator animator = body.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                int left = animator.GetLayerIndex(PistolStanceAnimation.OffHandLeftLayer);
                int right = animator.GetLayerIndex(PistolStanceAnimation.OffHandRightLayer);
                Assert.Greater(left, 3, "Off Hand layers come after the orchestrator's fixed layers 1-3");
                Assert.Greater(right, 3);

                Assert.Less(HandGap(animator, mirror: false, left, 0f, right, 0f), GripDistance, "two-handed grip");
                Assert.Less(HandGap(animator, mirror: true, left, 0f, right, 0f), GripDistance, "mirrored grip");
                Assert.Greater(HandGap(animator, mirror: false, left, 1f, right, 0f), GripDistance * 2f,
                    "right-hand gun: the left arm leaves the grip");
                Assert.Greater(HandGap(animator, mirror: true, left, 0f, right, 1f), GripDistance * 2f,
                    "left-hand gun: the right arm leaves the grip");
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }

        /// <summary>
        /// The pistol legs stand bladed (hips ~53° off the root) and the torso twists back. Mirroring only the
        /// upper body flips that twist against unmirrored hips and puts the gun beside the body, so a left-hand
        /// pistol mirrors the legs too. Either way the grip sits in front of the body, on its centre line.
        /// </summary>
        [Test]
        public void Grip_SitsInFrontOfTheBody_InEitherHand()
        {
            GameObject body = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath));
            try
            {
                Animator animator = body.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                int left = animator.GetLayerIndex(PistolStanceAnimation.OffHandLeftLayer);
                int right = animator.GetLayerIndex(PistolStanceAnimation.OffHandRightLayer);
                foreach (bool mirror in new[] { false, true })
                {
                    HandGap(animator, mirror, left, 0f, right, 0f);
                    Vector3 grip = body.transform.InverseTransformPoint(Vector3.Lerp(
                        animator.GetBoneTransform(HumanBodyBones.LeftHand).position,
                        animator.GetBoneTransform(HumanBodyBones.RightHand).position, 0.5f));
                    Assert.Greater(grip.z, 0.25f, $"mirror {mirror}: grip {grip} is in front");
                    Assert.Less(Mathf.Abs(grip.x), 0.1f, $"mirror {mirror}: grip {grip} is on the centre line");
                }
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }

        private static float HandGap(Animator animator, bool mirror, int leftLayer, float leftWeight, int rightLayer, float rightWeight)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.SetInteger("CombatStance", 2);
            animator.SetFloat(PistolStanceAnimation.HoldParameter, 1f);
            animator.SetBool("MirrorUpperBody", mirror);
            animator.SetLayerWeight(1, 1f);
            animator.SetLayerWeight(leftLayer, leftWeight);
            animator.SetLayerWeight(rightLayer, rightWeight);
            animator.Play("Ranged Locomotion", 0, 0f);
            animator.Play(PistolStanceAnimation.AimIdleState, 1, 0f);
            animator.Update(0f);
            animator.enabled = false;
            return Vector3.Distance(
                animator.GetBoneTransform(HumanBodyBones.LeftHand).position,
                animator.GetBoneTransform(HumanBodyBones.RightHand).position);
        }

        private static BlendTree FindPistolTree(out BlendTree holdBlend)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimatorState ranged = controller.layers[0].stateMachine.states
                .Select(s => s.state)
                .First(s => s.name == "Ranged Locomotion");
            holdBlend = (BlendTree)ranged.motion;
            return (BlendTree)holdBlend.children[1].motion;
        }

        private static float PlanarSpeed(AnimationClip clip) =>
            new Vector2(clip.averageSpeed.x, clip.averageSpeed.z).magnitude;

        private static AnimationClip LoadClip(PistolStanceAnimation.Clip entry) =>
            AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/Animations/{PistolStanceAnimation.Folder}/{entry.File}")
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => clip.name == entry.ClipName);
    }
}
