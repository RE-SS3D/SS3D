using System.Collections;
using System.Diagnostics;
using System.Linq;
using FishNet;
using FishNet.Component.Animating;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;

namespace SS3D.Tests
{
    /// <summary>
    /// What the environment does to a body: toxic air drives it critical, and a critical or dead ragdoll keeps
    /// gravity on the deck and loses it in space. Replaces the lifecycle pilot's ragdoll-space and
    /// critical-reclaim partials.
    /// </summary>
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Environment)]
    public class EnvironmentScenarios : ScenarioFixture
    {
        /// <summary>Plasma added at the body's tile. The main room holds about 2,600 mol of station air.</summary>
        private const int PlasmaMoles = 10000;

        // Guards: room air reaches the body's health tick and toxic air drives it critical, collapsing it with ragdoll gravity on the deck (health §environment, HumanHealthController.TickHealthFromEnvironment, Ragdoll) · Not cheaper because: needs the atmos simulation, the tile under the body and the server health tick together
        [UnityTest]
        public IEnumerator PlasmaInRoom_DrivesBodyCritical_CollapsesWithGravity()
        {
            ChangesRoom = true;
            HumanHealthController health = Body.GetComponent<HumanHealthController>();
            yield return Console.Expect($"atmosdebug gas plasma {PlasmaMoles}", "Added");

            Stopwatch exposure = Stopwatch.StartNew();
            yield return WaitFor(() => health.Snapshot.State == HealthState.Critical, "plasma drives the body critical", 60f);
            Debug.Log($"[Scenario] plasma made the body critical after {exposure.Elapsed.TotalSeconds:F1}s");
            Assert.IsTrue((health.Snapshot.CriticalFlags & HealthCriticalFlags.HighToxin) != 0, "Toxin is what made it critical");

            yield return WaitFor(() => Body.GetComponent<Ragdoll>().Presentation == BodyPresentationState.Collapsed,
                "the critical body collapses");
            AssertRagdollGravity(Body, true, "A body collapsed on the deck falls with gravity");
        }

        // Guards: a critical or dead ragdoll in open space floats near deck height and regains gravity back on the deck (the old ragdoll-space pilot) · Not cheaper because: needs ragdoll physics on a live networked body and the station's floor support
        [UnityTest]
        public IEnumerator CriticalAndDeadInSpace_RagdollIsWeightless()
        {
            Vector3 deck = Body.transform.position;
            Teleport(Body, new Vector3(10000f, deck.y, 10000f));
            Body.GetComponent<HumanoidBodyStateMachine>().SetFloating(true);
            yield return Hold(0.5f);
            Assert.AreEqual(HumanoidSupportState.Unsupported, HumanoidSpaceSupport.GetSupportAt(Body.transform.position),
                "The body is beyond the station's floor");

            MakeCritical(Body);
            AssertRagdollGravity(Body, false, "A critical ragdoll in space has no gravity");
            yield return AssertHipsHold("A critical ragdoll in space stays near deck height");

            yield return Console.Expect($"kill {ServerHelpers.LocalCkey}", "Player killed");
            yield return WaitFor(() => Body.GetComponent<Ragdoll>().Presentation == BodyPresentationState.Dead, "the body dies");
            AssertRagdollGravity(Body, false, "A corpse in space has no gravity");
            yield return AssertHipsHold("A corpse in space stays near deck height");

            Body.transform.position = deck;
            yield return Hold(0.5f);
            AssertRagdollGravity(Body, true, "A corpse back on the deck falls with gravity");
        }

        // Guards: a critical body keeps its collapse through ownership loss and reclaim, and treatment restores animation (reconnect while critical; the old critical-reclaim pilot) · Not cheaper because: needs FishNet ownership changes on a live body
        [UnityTest]
        public IEnumerator CriticalOwner_KeepsCollapseThroughReclaim_AndRecovers()
        {
            bool networkAnimatorWasEnabled = Body.GetComponent<NetworkAnimator>().enabled;
            MakeCritical(Body);
            Assert.IsTrue(BonesKinematic(false), "A collapsed owner simulates its bones");

            Body.RemoveOwnership();
            Body.Mind.RemoveOwnership();
            yield return WaitFor(() => !Body.IsOwner, "the host loses ownership");
            yield return Hold(0.5f);
            Assert.IsTrue(BonesKinematic(false), "Without an owner the server simulates the collapsed bones");
            Assert.IsFalse(Body.GetComponent<Animator>().enabled, "A collapsed body does not resume animation without an owner");

            Assert.IsTrue(SubSystems.Get<EntitySubSystem>().TryReclaimEntity(Host, InstanceFinder.ClientManager.Connection),
                "The host reclaims its body");
            yield return WaitFor(() => Body.IsOwner, "the host owns its body again");
            yield return Hold(0.5f);
            Assert.AreSame(Body, HostEntity, "Reclaim keeps the same body");
            Assert.AreEqual(BodyPresentationState.Collapsed, Body.GetComponent<Ragdoll>().Presentation, "Reclaim keeps the collapse");
            Assert.IsTrue(BonesKinematic(false), "The reclaiming owner simulates the bones again");

            // A late joiner sees the collapse SyncVar before OnStartNetwork runs.
            Body.GetComponent<Ragdoll>().OnStartNetwork();
            Assert.IsFalse(Body.GetComponent<NetworkAnimator>().enabled, "Network animation stays off while critical");

            yield return HealFully();
            HumanoidBodyStateMachine stateMachine = Body.GetComponent<HumanoidBodyStateMachine>();
            yield return WaitFor(() => stateMachine.enabled, "treatment restores body animation", 10f);
            yield return WaitFor(() => BonesKinematic(true), "recovered bones return to the animator");
            Assert.IsTrue(Body.GetComponent<Animator>().enabled, "Recovery enables the Animator");
            Assert.AreEqual(networkAnimatorWasEnabled, Body.GetComponent<NetworkAnimator>().enabled,
                "Recovery restores the NetworkAnimator as it was");
        }

        // Guards: server wind reaches the physics owner, collapses a standing body and accelerates its bones.
        // Not cheaper because: FishNet TargetRpc delivery and non-kinematic ragdoll ownership are runtime contracts.
        [UnityTest]
        public IEnumerator ViolentGust_KnocksDownAndPullsOwner_ThenRecovers()
        {
            Ragdoll ragdoll = Body.GetComponent<Ragdoll>();
            Vector3 start = ragdoll.WindSamplePosition;
            ragdoll.ServerApplyWind(Vector3.right * 30f);
            yield return WaitFor(() => ragdoll.Presentation == BodyPresentationState.Collapsed,
                "a violent gust knocks the owner down");
            yield return Hold(0.25f);
            Assert.That(ragdoll.WindSamplePosition.x, Is.GreaterThan(start.x + 0.1f),
                "the owning ragdoll moves with the gust");
            Assert.IsTrue(BonesKinematic(false), "Only the owner runs living ragdoll physics");
            yield return WaitFor(() => ragdoll.Presentation == BodyPresentationState.Locomotion,
                "the conscious body recovers after the wind stops", 8f);
        }

        // Guards: wind wakes the disabled-by-prefab Ragdoll and pushes an upright body a little every
        // frame instead of one jump per atmos tick.
        // Not cheaper because: component enable state and CharacterController motion need a live body.
        [UnityTest]
        public IEnumerator GentleWind_DriftsUprightOwner_InSmallSteps()
        {
            Ragdoll ragdoll = Body.GetComponent<Ragdoll>();
            Vector3 start = Body.transform.position;
            Vector3 previous = start;
            float largestStep = 0f;
            float nextUpdate = 0f;
            for (float elapsed = 0f; elapsed < 1f; elapsed += Time.deltaTime)
            {
                if (elapsed >= nextUpdate)
                {
                    ragdoll.ServerApplyWind(Vector3.right * 5f);
                    nextUpdate += AtmosConstants.TickInterval;
                }

                yield return null;
                Vector3 position = Body.transform.position;
                largestStep = Mathf.Max(largestStep, Vector3.Distance(position, previous));
                previous = position;
            }

            Assert.That(ragdoll.Presentation, Is.EqualTo(BodyPresentationState.Locomotion), "below knockdown strength");
            Assert.That(previous.x, Is.GreaterThan(start.x + 0.5f), "the body drifts with the wind");
            Assert.That(largestStep, Is.LessThan(0.25f), "no per-tick jumps");
        }

        // Guards: a real hull breach reaches both consumers: the shared flow atlas shows wind toward the hole
        // and client streaks appear from it, with no wind traffic of their own.
        // Not cheaper because: needs the tile observer, the host atlas and the render context running together.
        [UnityTest]
        public IEnumerator HullBreach_FillsFlowAtlas_AndShowsWindStreaks()
        {
            ChangesRoom = true;
            AtmosWindView view = SubSystems.Get<AtmosSubSystem>().GetComponent<AtmosWindView>();
            Assert.IsNotNull(view, "Clients get a wind view");
            Assert.That(view.VisibleStreaks, Is.Zero, "Sealed rooms have no wind");

            var breach = new Vector3(ScenarioAtmos.MainRoom.xMin - 1, 0f, -1f);
            Assert.IsTrue(SubSystems.Get<TileSubSystem>().Construction.TryClearTile(breach, TileLayer.Turf, Direction.North).Success,
                "The hull wall is removed");

            yield return WaitFor(() => view.VisibleStreaks > 0, "wind streaks appear after the breach", 5f);

            Vector2 flow = view.SampleFlow(ScenarioAtmos.MainRoom.xMin, -1f);
            Assert.That(flow.x, Is.LessThan(-0.1f), "Flow beside the hole points out through it");
            Ragdoll ragdoll = Body.GetComponent<Ragdoll>();
            Vector3 bodyStart = ragdoll.WindSamplePosition;
            yield return Hold(1.5f);
            // The test room's chunk borders run through the body's tile, so this also guards border links.
            Assert.That(Vector3.Distance(ragdoll.WindSamplePosition, bodyStart), Is.GreaterThan(0.3f),
                "The wind carries the body toward the breach");
        }

        private bool BonesKinematic(bool kinematic) => Body.GetComponentsInChildren<RagdollPart>(true)
            .All(part => part.GetComponent<Rigidbody>().isKinematic == kinematic);

        private static void AssertRagdollGravity(Entity body, bool gravity, string what)
        {
            Rigidbody[] bones = body.GetComponentsInChildren<RagdollPart>(true).Select(part => part.GetComponent<Rigidbody>()).ToArray();
            Assert.IsNotEmpty(bones, "The body has ragdoll bones");
            Assert.IsTrue(bones.All(bone => !bone.isKinematic && bone.useGravity == gravity), what);
        }

        private IEnumerator AssertHipsHold(string what)
        {
            float start = Hips();
            yield return Hold(3f);
            Assert.Less(Mathf.Abs(Hips() - start), 1f, what);
        }

        private float Hips() => Body.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips).position.y;
    }
}
