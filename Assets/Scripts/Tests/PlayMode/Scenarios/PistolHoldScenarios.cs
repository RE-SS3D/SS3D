using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a held taser in Harm reaches the pistol pose through the bridge, snapshot, orchestrator and IK, and a full off-hand takes that arm out of the grip (humanoid animation pass Phase 2) · Not cheaper because: the pose comes from the live hand containers, the networked snapshot and post-animation IK together
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Combat)]
    public class PistolHoldScenarios : ScenarioFixture
    {
        private const string Taser = "b35a2498ed516c0ad8a703bd70069a85";

        /// <summary>The left hand mirrors the whole pose, legs included (PistolStanceAnimation.StrafeParameter).</summary>
        [UnityTest]
        public IEnumerator Taser_GripsWithBothHands_UntilTheOffHandIsFull([Values(HandSide.Left, HandSide.Right)] HandSide side)
        {
            Hands hands = Body.GetComponent<Hands>();
            Hand gunHand = TwoHandedWeaponRules.FindHand(hands, side);
            Hand offHand = hands.PlayerHands.First(hand => hand != gunHand);
            hands.ServerSelectHand(gunHand);
            ItemSubSystem items = SubSystems.Get<ItemSubSystem>();
            Assert.IsTrue(gunHand.Container.AddItem(items.SpawnItem(Taser, Body.transform.position, Quaternion.identity)), "The taser goes in the hand");
            HumanoidBodyStateMachine body = Body.GetComponent<HumanoidBodyStateMachine>();
            body.CmdSetCombatMode(HumanoidCombatMode.Ranged);

            Animator animator = Body.GetComponent<Animator>();
            yield return WaitFor(() => body.Snapshot.PistolHold && IsUpperBodyIn(animator, PistolStanceAnimation.AimIdleState),
                "Harm with a taser plays the pistol idle");
            yield return Hold(0.5f);
            yield return new WaitForEndOfFrame();
            RangedWeaponItemExtension taser = gunHand.ItemInHand.GetComponent<RangedWeaponItemExtension>();
            taser.GetMuzzleWorldPose(out _, out Vector3 muzzleForward);
            float gripGap = HandGap(animator);
            float aimAngle = Vector3.Angle(Flat(muzzleForward), Flat(Body.transform.forward));
            Debug.Log($"[PistolHoldScenarios] {gunHand.Side} hand: grip gap {gripGap:F3} m, muzzle {aimAngle:F1}° off body forward");
            Assert.Less(gripGap, 0.15f, "Both hands hold the grip");
            Assert.Less(aimAngle, 30f, "The taser points where the body faces");

            Assert.IsTrue(offHand.Container.AddItem(items.SpawnItem(Taser, Body.transform.position, Quaternion.identity)), "A second item fills the off-hand");
            yield return WaitFor(() => body.Snapshot.OffHandBusy, "the snapshot carries the full off-hand");
            yield return Hold(0.5f);
            yield return new WaitForEndOfFrame();
            float freeGap = HandGap(animator);
            Debug.Log($"[PistolHoldScenarios] off-hand gap {freeGap:F3} m");
            Assert.Greater(freeGap, 0.25f, "The full off-hand leaves the grip");
            Assert.IsTrue(IsUpperBodyIn(animator, PistolStanceAnimation.AimIdleState), "The gun arm keeps the pistol pose");
            EmptyHands(Body);
        }

        private static bool IsUpperBodyIn(Animator animator, string state) =>
            animator.GetCurrentAnimatorStateInfo(1).IsName(state);

        private static float HandGap(Animator animator) => Vector3.Distance(
            animator.GetBoneTransform(HumanBodyBones.LeftHand).position,
            animator.GetBoneTransform(HumanBodyBones.RightHand).position);

        private static Vector3 Flat(Vector3 direction) => new Vector3(direction.x, 0f, direction.z).normalized;
    }
}
