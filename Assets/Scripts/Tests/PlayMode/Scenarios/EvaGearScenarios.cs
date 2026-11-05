using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Combat;
using SS3D.Systems.Examine;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a hit that gets past a worn hardsuit punctures its seal, and the holder sees it on the alert stack and on examine (armor §3, §6; ApplyArmorAbsorption, ApplyWornSeal, AtmosAlertStackMapper, SealedGearItemExtension) · Not cheaper because: crosses console damage, worn containers, the item SyncVar and the health snapshot in one host
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.EvaGear)]
    public class EvaGearScenarios : ScenarioFixture
    {
        // Prefab GUIDs, the keys of the Items database.
        private const string Hardsuit = "e5e035169e67c766b8bc119007b84634";
        private const string HardsuitHelmet = "9ae653f88306f6f55bc8000fcbfc2b67";

        [UnityTest]
        public IEnumerator HitPastTheSuit_BreachesTheSeal()
        {
            HumanHealthController health = Body.GetComponent<HumanHealthController>();
            Item suit = Wear(Hardsuit, ContainerType.ExoSuit);
            Wear(HardsuitHelmet, ContainerType.Head);
            SealedGearItemExtension seal = suit.GetComponent<SealedGearItemExtension>();
            yield return WaitFor(() => health.Snapshot.Environment.IsSealed, "the worn suit and helmet seal");
            Assert.AreEqual(ExamineSealKeys.IntactFallback, ExamineLine(seal));

            yield return Console.Expect($"hurt {ServerHelpers.LocalCkey} Chest 5 0", "Player hurt");
            yield return WaitFor(() => suit.GetComponent<ArmorItemExtension>().Integrity < 160f, "the suit absorbs the hit");
            Assert.IsFalse(seal.IsBreached, "A fully absorbed hit leaves the seal alone");

            yield return Console.Expect($"hurt {ServerHelpers.LocalCkey} Chest 30 0", "Player hurt");
            yield return WaitFor(() => health.Snapshot.Environment.IsBreached, "the breach reaches the snapshot");

            Assert.IsTrue(seal.IsBreached, "Damage past the suit punctures it");
            Assert.IsFalse(health.Snapshot.Environment.IsSealed, "A breach breaks the whole seal");
            Assert.AreEqual(
                AtmosAlertStackMapper.Severity.Warning,
                AtmosAlertStackMapper.Compute(health.Snapshot.Environment).SealBreach,
                "Room air makes the breach a warning");
            Assert.AreEqual(ExamineSealKeys.BreachedFallback, ExamineLine(seal));
        }

        private Item Wear(string item, ContainerType slot)
        {
            AttachedContainer container = Body.GetComponent<HumanInventory>().Containers.Single(worn => worn.ContainerType == slot);
            return SubSystems.Get<ItemSubSystem>().SpawnItemInContainer(item, container);
        }

        private static string ExamineLine(SealedGearItemExtension seal)
        {
            List<ExamineSection> sections = new();
            seal.AppendSections(null, sections);
            return sections.Single().Text;
        }
    }
}
