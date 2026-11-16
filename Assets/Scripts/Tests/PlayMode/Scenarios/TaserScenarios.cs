using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Systems.Combat;
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
    // Guards: a taser hit floors a combat dummy for a knockdown (combat.md §5), the dummy stands up again still frozen, a second hit keeps it down without stacking, and an intact hardsuit (not a broken one) stops the knockdown · Not cheaper because: the unowned ragdoll, its server-side physics authority and the get-up clip only run on a spawned body
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Combat)]
    public class TaserScenarios : ScenarioFixture
    {
        // Examine reads the charge off a replicated count (examine.md §7), not the holder-only fire state;
        // a spawned NetworkBehaviour is the only place the SyncVar exists.
        [UnityTest]
        public IEnumerator TaserShot_LowersTheChargeExamineShows()
        {
            yield return Console.Expect("spawn Taser hand", "spawned in hand");
            Item item = Body.GetComponent<HumanInventory>().Hands.SelectedHand.ItemInHand;
            Assert.IsNotNull(item, "The taser is in hand");
            SS3D.Systems.Combat.Interactions.RangedWeaponItemExtension weapon =
                item.GetComponent<SS3D.Systems.Combat.Interactions.RangedWeaponItemExtension>();
            RangedWeaponRoundsReadout readout = item.GetComponent<RangedWeaponRoundsReadout>();
            Assert.IsNotNull(readout, "The taser prefab carries the rounds readout");

            Assert.AreEqual("Charge: 3 shots.", ExamineLine(readout), "A fresh taser examines full");
            Assert.IsTrue(weapon.ServerTryConsumeRound(), "The server spends a charge");
            yield return null;
            Assert.AreEqual("Charge: 2 shots.", ExamineLine(readout), "Examine follows the spent charge");
            item.Delete();
        }

        private static string ExamineLine(RangedWeaponRoundsReadout readout)
        {
            var sections = new System.Collections.Generic.List<SS3D.Systems.Examine.ExamineSection>();
            readout.AppendSections(null, sections);
            return sections.Single().Text;
        }

        [UnityTest]
        public IEnumerator TaserHit_KnocksDummyDown_ThenItStandsFrozen()
        {
            // Open floor in the main room; "two metres ahead" of the fresh body is inside its north wall.
            var spawn = new Vector3(0f, Body.transform.position.y, ScenarioAtmos.MainRoom.yMin + 1f);
            Entity dummy = SubSystems.Get<EntitySubSystem>().ServerSpawnCombatDummy(spawn, Quaternion.identity);
            yield return Hold(0.5f);
            Ragdoll ragdoll = dummy.GetComponent<Ragdoll>();
            HumanHealthController health = dummy.GetComponentInChildren<HumanHealthController>();
            RangedWeaponProfile taser = RangedWeaponProfile.Taser;

            CombatInteractionNetwork.ServerApplyLivingHit(taser, health, BodyZone.Chest);
            Assert.IsTrue(ragdoll.IsKnockedDown, "The taser floors the dummy");
            Assert.That(dummy.GetComponent<StaminaController>().CurrentStamina, Is.EqualTo(0.35f).Within(0.05f),
                "An unarmored hit drains 65% of a full bar");

            yield return Hold(1f);
            CombatInteractionNetwork.ServerApplyLivingHit(taser, health, BodyZone.Chest);
            yield return Hold(taser.KnockdownSeconds - 0.3f);
            Assert.IsTrue(ragdoll.IsKnockedDown, "A second hit refreshes the knockdown");

            yield return WaitFor(() => !ragdoll.IsKnockedDown, "The dummy gets up", timeout: 1f);
            Assert.That(ragdoll.Presentation, Is.EqualTo(BodyPresentationState.Locomotion));
            yield return WaitFor(() => !ragdoll.IsGettingUp, "The get-up finishes", timeout: 5f);
            yield return Hold(0.2f);
            Assert.IsFalse(dummy.GetComponent<HumanoidLivingController>().enabled, "Standing up leaves the dummy frozen");
            Assert.That(dummy.transform.position.y, Is.EqualTo(spawn.y).Within(0.1f), "It stands on the deck");
        }

        [UnityTest]
        public IEnumerator TaserHit_OnIntactHardsuit_DrainsHalfStaminaWithoutKnockdown()
        {
            Entity dummy = SpawnDummy();
            Wear(dummy, Items.SyndicateHardsuit, ContainerType.ExoSuit);
            yield return Hold(0.5f);
            StaminaController stamina = dummy.GetComponent<StaminaController>();
            float before = stamina.CurrentStamina;

            CombatInteractionNetwork.ServerApplyLivingHit(RangedWeaponProfile.Taser,
                dummy.GetComponentInChildren<HumanHealthController>(), BodyZone.Chest);
            yield return Hold(0.2f);

            Assert.IsFalse(dummy.GetComponent<Ragdoll>().IsKnockedDown, "The hardsuit stops the prongs");
            // CurrentStamina is a 0..1 share of the bar; the drain is in absolute points of StaminaFactory.DefaultMax.
            float expected = RangedWeaponProfile.Taser.StaminaDamage * RangedWeaponProfile.StunBlockedStaminaFactor
                / StaminaFactory.DefaultMax;
            Assert.That(before - stamina.CurrentStamina, Is.EqualTo(expected).Within(0.05f), "Half the stamina drain still lands");
        }

        [UnityTest]
        public IEnumerator TaserHit_OnBrokenHardsuit_OrUncoveredHead_KnocksDown([Values(true, false)] bool brokenSuit)
        {
            Entity dummy = SpawnDummy();
            Item suit = Wear(dummy, Items.SyndicateHardsuit, ContainerType.ExoSuit);
            yield return Hold(0.5f);
            if (brokenSuit)
            {
                ArmorItemExtension armor = suit.GetComponent<ArmorItemExtension>();
                while (armor.Integrity > 0f)
                    armor.ServerAbsorb(100f, 0f);
            }

            CombatInteractionNetwork.ServerApplyLivingHit(RangedWeaponProfile.Taser,
                dummy.GetComponentInChildren<HumanHealthController>(), brokenSuit ? BodyZone.Chest : BodyZone.Head);

            Assert.IsTrue(dummy.GetComponent<Ragdoll>().IsKnockedDown,
                brokenSuit ? "A spent hardsuit no longer stops the taser" : "The suit does not cover a bare head");
        }

        private Entity SpawnDummy()
        {
            var spawn = new Vector3(0f, Body.transform.position.y, ScenarioAtmos.MainRoom.yMin + 1f);
            return SubSystems.Get<EntitySubSystem>().ServerSpawnCombatDummy(spawn, Quaternion.identity);
        }

        private static Item Wear(Entity body, string item, ContainerType slot)
        {
            AttachedContainer container = body.GetComponent<HumanInventory>().Containers.Single(worn => worn.ContainerType == slot);
            return SubSystems.Get<ItemSubSystem>().SpawnItemInContainer(item, container);
        }
    }
}
