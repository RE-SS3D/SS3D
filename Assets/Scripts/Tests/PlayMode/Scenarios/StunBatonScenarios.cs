using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Stamina;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a stun baton hit floors a combat dummy and drains stamina (combat.md §5), and an intact hardsuit that stops the taser does not stop the baton · Not cheaper because: the knockdown lives on a spawned body's Ragdoll and its server-side physics authority
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Combat)]
    public class StunBatonScenarios : ScenarioFixture
    {
        [UnityTest]
        public IEnumerator BatonHit_KnocksDownThroughAHardsuit_AndDrainsStamina()
        {
            var spawn = new Vector3(0f, Body.transform.position.y, ScenarioAtmos.MainRoom.yMin + 1f);
            Entity dummy = SubSystems.Get<EntitySubSystem>().ServerSpawnCombatDummy(spawn, Quaternion.identity);
            AttachedContainer suitSlot = dummy.GetComponent<HumanInventory>().Containers.Single(c => c.ContainerType == ContainerType.ExoSuit);
            SubSystems.Get<ItemSubSystem>().SpawnItemInContainer(Items.SyndicateHardsuit, suitSlot);
            yield return Hold(0.5f);
            StaminaController stamina = dummy.GetComponent<StaminaController>();
            float before = stamina.CurrentStamina;

            MeleeWeaponProfile baton = MeleeWeaponProfile.StunBaton;
            MeleeHitInteraction.ServerApplyLivingHit(baton, dummy.GetComponentInChildren<HumanHealthController>(), BodyZone.Chest);
            yield return Hold(0.2f);

            Assert.IsTrue(dummy.GetComponent<Ragdoll>().IsKnockedDown, "A contact stun ignores the hardsuit that stops taser prongs");
            // CurrentStamina is a 0..1 share of the bar; the drain is in absolute points of StaminaFactory.DefaultMax.
            Assert.That(before - stamina.CurrentStamina, Is.EqualTo(baton.StaminaDamage / StaminaFactory.DefaultMax).Within(0.05f),
                "The full stamina drain lands");
        }
    }
}
