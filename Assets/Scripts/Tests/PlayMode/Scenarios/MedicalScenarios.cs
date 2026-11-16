using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Interactions;
using SS3D.Systems.Health;
using SS3D.Systems.Interactions;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a bleeding wound can be bandaged by the wounded player (health §treatment, BandageInteraction) · Not cheaper because: crosses console spawn, inventory, the interaction RPC and the health tick in one host
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Medical)]
    public class MedicalScenarios : ScenarioFixture
    {
        [UnityTest]
        public IEnumerator BandageOnSelf_StopsBleeding([Values(BodyZone.Chest, BodyZone.LeftLeg)] BodyZone zone)
        {
            HumanHealthController health = Body.GetComponent<HumanHealthController>();
            yield return Console.Expect("spawn MedicalPatch hand", "spawned in hand");
            yield return Console.Expect($"hurt {ServerHelpers.LocalCkey} {zone} 30 0", "Player hurt");
            yield return WaitFor(() => health.Snapshot.IsZoneBleeding(zone), $"the {zone} wound bleeds");

            InteractionController interactions = Body.GetComponent<InteractionController>();
            string refusal = interactions.RunInteractionForTesting(Body.gameObject, ZoneCentre(zone), "Bandage", IntentType.Help);
            Assert.IsNull(refusal, refusal);

            yield return WaitFor(() => !health.Snapshot.IsZoneBleeding(zone), $"the bandage stops the {zone} bleeding");
        }

        private Vector3 ZoneCentre(BodyZone zone)
        {
            ZoneTargetCollider target = Body.GetComponentsInChildren<ZoneTargetCollider>(true).FirstOrDefault(c => c.Zone == zone);
            Assert.IsNotNull(target, $"The body has a {zone} zone collider");
            return target.GetComponent<Collider>().bounds.center;
        }
    }
}
