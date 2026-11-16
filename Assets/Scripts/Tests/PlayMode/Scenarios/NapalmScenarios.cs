using System.Collections;
using FishNet;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: deposited fuel hurts a real body and reaches the observer after firing stops, then expires.
    // Not cheaper because: the world-owned clock, health authority and buffered observer RPC need a spawned hub.
    [Category(TestCategories.Scenarios)]
    [Category("Napalm")]
    public sealed class NapalmScenarios : ScenarioFixture
    {
        [UnityTest]
        public IEnumerator DepositedFuel_BurnsWithoutAShooter_ThenExpires()
        {
            NapalmSubSystem napalm = SubSystems.Get<NapalmSubSystem>();
            HumanHealthController health = Body.GetComponent<HumanHealthController>();
            EntityInvincibility.ClearServer(InstanceFinder.ClientManager.Connection.ClientId);
            EntityInvincibility.LocalPlayerInvincible = false;
            Vector3 feet = Body.transform.position;
            Assert.IsTrue(Physics.Raycast(feet + Vector3.up * 0.3f, Vector3.down, out RaycastHit floor, 1f,
                LayerMask.GetMask("Default", "Walls"), QueryTriggerInteraction.Ignore));
            float before = health.Snapshot.WorstZoneBurn;
            try
            {
                napalm.Deposit(floor);
                yield return WaitFor(() => health.Snapshot.WorstZoneBurn > before, "surface fuel burns after firing stops", 3f);
                yield return WaitFor(() => Object.FindObjectsByType<NapalmSurfaceVfx>(FindObjectsSortMode.None).Length > 0,
                    "the observer sees the persistent patch", 3f);
                Teleport(Body, feet + Vector3.right * 3f);
                yield return Hold(NapalmPatchSet.BurnSeconds + 1f);
                float after = health.Snapshot.WorstZoneBurn;
                yield return Hold(1f);
                Assert.AreEqual(after, health.Snapshot.WorstZoneBurn, 0.01f, "Expired fuel stops dealing damage.");
                NapalmSurfaceVfx[] scars = Object.FindObjectsByType<NapalmSurfaceVfx>(FindObjectsSortMode.None);
                Assert.IsNotEmpty(scars, "Extinguished surface fire leaves a black scorch.");
                foreach (NapalmSurfaceVfx view in scars)
                {
                    Assert.IsFalse(view.IsBurning);
                    Assert.IsFalse(view.IsBodyFire);
                    Assert.IsTrue(view.HasScorch, "Surface scorches are URP decals.");
                    Assert.AreEqual(1f, view.ScorchReveal, 0.001f, "The char is complete once the flames are out.");
                }
            }
            finally { napalm.ClearFuel(); }
        }

        [UnityTest]
        public IEnumerator WalkingIntoFire_IgnitesTheBody_AndKeepsBurningAfterLeaving()
        {
            NapalmSubSystem napalm = SubSystems.Get<NapalmSubSystem>();
            HumanHealthController health = Body.GetComponent<HumanHealthController>();
            EntityInvincibility.ClearServer(InstanceFinder.ClientManager.Connection.ClientId);
            EntityInvincibility.LocalPlayerInvincible = false;
            Vector3 origin = Body.transform.position;
            Assert.IsTrue(Physics.Raycast(origin + Vector3.up * 0.3f, Vector3.down, out RaycastHit floor, 1f,
                LayerMask.GetMask("Default", "Walls"), QueryTriggerInteraction.Ignore));
            float before = health.Snapshot.WorstZoneBurn;
            try
            {
                Teleport(Body, origin + Vector3.right * 2f);
                napalm.Deposit(floor);
                yield return Hold(0.5f);
                Assert.AreEqual(before, health.Snapshot.WorstZoneBurn, "Standing clear of the pool is safe.");
                Teleport(Body, origin + Vector3.right * 0.8f);
                yield return WaitFor(() => health.Snapshot.WorstZoneBurn > before, "walking into flame ignites the body", 4f);
                NapalmSurfaceVfx bodyFire = null;
                yield return WaitFor(() =>
                {
                    foreach (NapalmSurfaceVfx view in Object.FindObjectsByType<NapalmSurfaceVfx>(FindObjectsSortMode.None))
                        if (view.IsBodyFire) { bodyFire = view; return true; }
                    return false;
                }, "a coating follows the burning body", 3f);
                Assert.IsTrue(bodyFire.transform.IsChildOf(Body.transform));
                float onExit = health.Snapshot.WorstZoneBurn;
                Teleport(Body, origin + Vector3.right * 3f);
                yield return WaitFor(() => health.Snapshot.WorstZoneBurn > onExit, "napalm keeps burning away from the pool", 3f);
                yield return Hold(NapalmPatchSet.BurnSeconds + 1f);
                float after = health.Snapshot.WorstZoneBurn;
                yield return Hold(1f);
                Assert.AreEqual(after, health.Snapshot.WorstZoneBurn, 0.01f, "The coating eventually burns out.");
                foreach (NapalmSurfaceVfx view in Object.FindObjectsByType<NapalmSurfaceVfx>(FindObjectsSortMode.None))
                    Assert.IsFalse(view.IsBodyFire, "Body flames disappear; surface scorch remains.");
            }
            finally { napalm.ClearFuel(); }
        }
    }
}
