using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: an M4 burst puts a combat dummy Downed (crawling, not ragdolled) before Critical, bandaging lets it stand
    // again on the stand line, a second player can help it up sooner, and a hardsuit takes more rounds (health.md §4,
    // shock-and-downed plan PR 6) · Not cheaper because: the 1 Hz health tick, Ragdoll's presentation SyncVar, the body
    // state machine's Crawling state and the help-up interaction only meet on spawned, networked bodies
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Combat)]
    public class ShockScenarios : ScenarioFixture
    {
        private static readonly BodyZone[] CenterMass = { BodyZone.Chest, BodyZone.Groin };

        /// <summary>Seconds for pending pain to become shock: the build window plus a tick of slack.</summary>
        private const float SettleSeconds = HealthConstants.ShockBuildSeconds + 1.5f;

        [UnityTest]
        public IEnumerator M4Burst_DownsTheDummy_BandagesStandItUp_ASecondBurstMakesItCritical()
        {
            Entity dummy = SpawnDummy();
            yield return Undress(dummy);
            HumanHealthController health = dummy.GetComponentInChildren<HumanHealthController>();
            Ragdoll ragdoll = dummy.GetComponent<Ragdoll>();
            HumanoidBodyStateMachine body = dummy.GetComponent<HumanoidBodyStateMachine>();

            Burst(health, 7);
            Assert.AreEqual(HealthState.Healthy, health.Snapshot.State, "Shock builds over a few seconds, not on the hit");
            Assert.Greater(health.Snapshot.IncomingPain, 0f, "The snapshot carries the pain on its way");
            yield return WaitFor(() => health.Snapshot.State == HealthState.Downed, "seven M4 rounds put the dummy down", SettleSeconds);

            Assert.IsTrue(ragdoll.IsDowned, "Downed is its own presentation");
            Assert.IsFalse(ragdoll.IsKnockedDown, "Downed is not a ragdoll");
            Assert.AreEqual(BodyState.Crawling, body.Snapshot.State);
            Assert.IsTrue(body.Snapshot.IsCrawling, "The crawl clip plays");
            Assert.IsTrue(health.Snapshot.IsConscious && health.Snapshot.CanUseArms, "Downed is conscious and keeps its hands");

            Animator animator = dummy.GetComponent<Animator>();
            Assert.IsTrue(ragdoll.IsFallingDowned, "A standing body falls before it lies Downed");
            Assert.IsFalse(ragdoll.IsKnockedDown, "The fall is the Dying clip, not a ragdoll");
            yield return null;
            Assert.IsTrue(animator.GetNextAnimatorStateInfo(DownedAnimation.Layer).IsName(DownedAnimation.FallStanding.StateName)
                || animator.GetCurrentAnimatorStateInfo(DownedAnimation.Layer).IsName(DownedAnimation.FallStanding.StateName),
                "A body standing still plays the standing fall");
            float fallSeconds = (DownedAnimation.FallStanding.LastFrame - DownedAnimation.FallStanding.FirstFrame) / 30f;
            yield return WaitFor(() => !ragdoll.IsFallingDowned, "the fallen body blends into the Downed pose",
                fallSeconds + GetUpAnimation.BlendInSeconds + 1f);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(DownedAnimation.Layer).IsName(DownedAnimation.CrawlState),
                "The standing fall ends belly down, in the crawl state");
            yield return null;
            float hipsHeight = animator.GetBoneTransform(HumanBodyBones.Hips).position.y - dummy.transform.position.y;
            Assert.Less(hipsHeight, 0.3f, "The Downed pose lies on the floor, not in the air");

            foreach (BodyZone zone in CenterMass)
            {
                health.ApplyTreatment(zone, stopBleeding: true);
            }

            float secondsToStand = (health.Snapshot.Shock - HealthConstants.ShockStandThreshold) / HealthConstants.ShockDecayPerSecond;
            yield return WaitFor(() => health.Snapshot.State == HealthState.Healthy, "the bandaged dummy stands on its own", secondsToStand + 3f);
            yield return WaitFor(() => ragdoll.Presentation == BodyPresentationState.Locomotion && body.Snapshot.State == BodyState.Locomotion,
                "the body is upright again", 2f);
            Assert.IsTrue(ragdoll.IsGettingUp, "Leaving Downed plays the get-up rather than snapping upright");
            Assert.IsFalse(body.Snapshot.IsCrawling);

            Burst(health, 14);
            yield return WaitFor(() => health.Snapshot.State == HealthState.Critical, "a further burst makes it critical", SettleSeconds);
            Assert.IsTrue((health.Snapshot.CriticalFlags & HealthCriticalFlags.HighShock) != 0, "Shock is why");
            Assert.IsTrue(ragdoll.IsKnockedDown, "Critical collapses into the ragdoll");
        }

        [UnityTest]
        public IEnumerator HardsuitDummy_TakesMoreRoundsToGoDown()
        {
            Entity dummy = SpawnDummy();
            yield return Undress(dummy);
            AttachedContainer suitSlot = dummy.GetComponent<HumanInventory>().Containers.Single(worn => worn.ContainerType == ContainerType.ExoSuit);
            SubSystems.Get<ItemSubSystem>().SpawnItemInContainer(Items.SyndicateHardsuit, suitSlot);
            yield return Hold(0.5f);
            HumanHealthController health = dummy.GetComponentInChildren<HumanHealthController>();

            Burst(health, 7);
            yield return Hold(SettleSeconds);
            Assert.AreEqual(HealthState.Healthy, health.Snapshot.State, "Seven rounds that would down a bare body leave the suited one up");
            Assert.Greater(health.Snapshot.Shock, HealthConstants.ShockWarningThreshold, "but it is hurting");

            Burst(health, 5, offset: 7);
            yield return WaitFor(() => health.Snapshot.State == HealthState.Downed, "a focused response still downs it", SettleSeconds);
        }

        [UnityTest]
        public IEnumerator HelpUp_StandsADownedBodyUnderTheDownedLine_ThenItIsUnsteady()
        {
            Entity dummy = SpawnDummy();
            Teleport(Body, dummy.transform.position + (Vector3.right * 0.9f));
            EmptyHands(Body);
            yield return Undress(dummy);
            HumanHealthController health = dummy.GetComponentInChildren<HumanHealthController>();

            Burst(health, 7);
            yield return WaitFor(() => health.Snapshot.State == HealthState.Downed, "the burst downs the dummy", SettleSeconds);
            InteractionController helper = Body.GetComponent<InteractionController>();
            Assert.IsNotNull(Run(helper, dummy), "Too much shock: no help up yet");

            foreach (BodyZone zone in CenterMass)
            {
                health.ApplyTreatment(zone, stopBleeding: true);
            }

            yield return WaitFor(() => health.Snapshot.Shock < HealthConstants.ShockDownedThreshold - 1f,
                "shock falls under the Downed line", 10f);
            Assert.AreEqual(HealthState.Downed, health.Snapshot.State, "Between the lines it would stay down on its own");
            Assert.IsNull(Run(helper, dummy), "An empty hand on Help intent offers Help up");
            yield return WaitFor(() => health.Snapshot.State == HealthState.Healthy, "the helper pulls it up", HealthConstants.HelpUpSeconds + 2f);
            Assert.IsTrue(health.Snapshot.IsUnsteady, "Helped up, it is unsteady");
            Assert.IsFalse(health.Snapshot.CanSprint);
            yield return WaitFor(() => !health.Snapshot.IsUnsteady, "the unsteadiness wears off", HealthConstants.UnsteadySeconds + 2f);
        }

        private static string Run(InteractionController helper, Entity target) =>
            helper.RunInteractionForTesting(target.gameObject, target.transform.position + (Vector3.up * 0.3f), "Help up", IntentType.Help);

        /// <summary>M4 rounds landing one after another, alternating chest and groin.</summary>
        private static void Burst(HumanHealthController health, int rounds, int offset = 0)
        {
            for (int i = 0; i < rounds; i++)
            {
                CombatInteractionNetwork.ServerApplyLivingHit(RangedWeaponProfile.M4, health, CenterMass[(i + offset) % CenterMass.Length]);
            }
        }

        private Entity SpawnDummy()
        {
            var spawn = new Vector3(0f, Body.transform.position.y, ScenarioAtmos.MainRoom.yMin + 1f);
            return SubSystems.Get<EntitySubSystem>().ServerSpawnCombatDummy(spawn, Quaternion.identity);
        }

        /// <summary>Takes off the dummy's security jumpsuit, which is armor: the M4 targets are for a bare body.</summary>
        private static IEnumerator Undress(Entity dummy)
        {
            yield return Hold(0.5f);
            foreach (Item worn in dummy.GetComponent<HumanInventory>().Containers
                .Where(container => container.ContainerType == ContainerType.Jumpsuit)
                .SelectMany(container => container.Items).ToList())
            {
                worn.Delete();
            }

            yield return Hold(0.3f);
        }
    }
}
