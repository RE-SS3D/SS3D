using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Examine;
using SS3D.Systems.Health;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
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
        private const string GasMask = "bf177f8bab26bc84b87fd9a971004418";
        private const string OxygenTank = "5479411218b869144b7d09d6db6dc58e";
        private const string EmergencyTank = "e220e4181141f914aa994d7672ed6ec5";
        private const string Magboots = "2ba59995b958400888f44509d7530731";
        private const string InsulatedGloves = "65679cf5a30d6dd458666804af7c3ca3";

        [UnityTest]
        public IEnumerator HitPastTheSuit_BreachesTheSeal()
        {
            HumanHealthController health = Body.GetComponent<HumanHealthController>();
            Item suit = Wear(Hardsuit, ContainerType.ExoSuit);
            Wear(HardsuitHelmet, ContainerType.Head);
            SealedGearItemExtension seal = suit.GetComponent<SealedGearItemExtension>();
            Assert.AreEqual(160f, suit.GetComponent<ArmorItemExtension>().Integrity, "The suit starts at its authored integrity");
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

        [UnityTest]
        public IEnumerator Internals_FeedFromSuitStorageUntilTheTankLeaves()
        {
            HumanHealthController health = Body.GetComponent<HumanHealthController>();
            EvaGearController gear = Body.GetComponent<EvaGearController>();
            HumanInventory inventory = Body.GetComponent<HumanInventory>();
            AttachedContainer storage = inventory.Containers.Single(worn => worn.ContainerType == ContainerType.SuitStorage);
            inventory.Containers.Single(worn => worn.ContainerType == ContainerType.Pocket).Dump();
            Item tank = SubSystems.Get<ItemSubSystem>().SpawnItem(OxygenTank, Vector3.up, Quaternion.identity);
            Assert.IsFalse(storage.CanContainItem(tank), "Suit storage needs a worn suit");

            Item suit = Wear(Hardsuit, ContainerType.ExoSuit);
            Wear(HardsuitHelmet, ContainerType.Head);
            storage.AddItem(tank);
            GasTankItemExtension supply = tank.GetComponent<GasTankItemExtension>();
            yield return WaitFor(() => gear.InternalsAvailable, "a sealed helmet and a tank offer internals");
            Assert.IsFalse(gear.InternalsOn, "Internals are a deliberate switch");
            Assert.IsFalse(suit.Container.CanRemoveItem(suit), "A suit stays on while its storage holds a tank");

            gear.ServerSetInternals(true);
            yield return WaitFor(() => health.Snapshot.Environment.OnInternals, "the breath comes from the tank");
            yield return WaitFor(() => supply.Fill < 1f, "breathing draws on the tank");

            supply.ServerDraw(supply.Contents.TotalMoles);
            yield return WaitFor(() => health.Snapshot.Environment.TankFill <= 0f, "the snapshot sees the empty tank");
            AtmosAlertStackMapper.AtmosAlertSignals alerts = AtmosAlertStackMapper.Compute(health.Snapshot.Environment);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.Critical, alerts.LowOxygen, "An empty tank is not room air");
            Assert.AreEqual(AtmosAlertStackMapper.Severity.Critical, alerts.TankLow);

            storage.RemoveItem(tank);
            yield return WaitFor(() => !gear.InternalsOn, "losing the tank turns internals off");
            Assert.IsFalse(gear.InternalsAvailable);
            Assert.IsTrue(suit.Container.CanRemoveItem(suit), "An empty slot frees the suit");
        }

        [UnityTest]
        public IEnumerator Mask_NeedsASmallTankInThePocket()
        {
            EvaGearController gear = Body.GetComponent<EvaGearController>();
            AttachedContainer pocket = Body.GetComponent<HumanInventory>().Containers.Single(worn => worn.ContainerType == ContainerType.Pocket);
            pocket.Dump();
            Item full = SubSystems.Get<ItemSubSystem>().SpawnItem(OxygenTank, Vector3.up, Quaternion.identity);
            Assert.IsFalse(pocket.CanContainItem(full), "A full tank is too big for a pocket");

            Wear(GasMask, ContainerType.Mask);
            yield return null;
            Assert.IsFalse(gear.InternalsAvailable, "A mask alone connects to nothing");

            SubSystems.Get<ItemSubSystem>().SpawnItemInContainer(EmergencyTank, pocket);
            yield return WaitFor(() => gear.InternalsAvailable, "a mask and a pocket tank offer internals");
        }

        [UnityTest]
        public IEnumerator ApplyOxygen_DrawsFromTheTankUntilItIsEmpty()
        {
            HumanHealthController health = Body.GetComponent<HumanHealthController>();
            yield return Console.Expect("spawninhand OxygenTank", "spawned in hand");
            GasTankItemExtension tank = Body.GetComponent<Hands>().SelectedHand.ItemInHand.GetComponent<GasTankItemExtension>();
            while (health.Snapshot.Pools.OxyDebt < HealthConstants.TreatmentOxyDebtThreshold)
            {
                health.TickHealth(atmosphereO2: 0f);
            }

            InteractionController interactions = Body.GetComponent<InteractionController>();
            Vector3 head = Body.GetComponentsInChildren<ZoneTargetCollider>(true)
                .First(target => target.Zone == BodyZone.Head)
                .GetComponent<Collider>().bounds.center;
            string refusal = interactions.RunInteractionForTesting(Body.gameObject, head, "Apply oxygen", IntentType.Help);
            Assert.IsNull(refusal, refusal);
            yield return WaitFor(() => tank.Fill < 1f, "the treatment draws on the tank");

            tank.ServerDraw(tank.Contents.TotalMoles);
            yield return null;
            Assert.IsNotNull(
                interactions.RunInteractionForTesting(Body.gameObject, head, "Apply oxygen", IntentType.Help),
                "An empty tank treats nobody");
        }

        [UnityTest]
        public IEnumerator Magboots_NeedBothFeetAndHoldAgainstWindUntilOnTheFloor()
        {
            EvaGearController gear = Body.GetComponent<EvaGearController>();
            Ragdoll ragdoll = Body.GetComponent<Ragdoll>();
            HumanoidLivingController movement = Body.GetComponent<HumanoidLivingController>();
            AttachedContainer feet = Worn(ContainerType.ShoeLeft);
            feet.Dump();

            Item boots = Wear(Magboots, ContainerType.ShoeLeft);
            yield return WaitFor(() => gear.MagbootsAvailable, "the pair offers magboots");
            Assert.IsFalse(gear.MagbootsOn, "Magboots are a deliberate switch");
            gear.ServerSetMagboots(true);
            yield return WaitFor(() => gear.MagbootsAnchored, "the boots hold");
            Assert.AreEqual(HealthConstants.MagbootSpeedFactor, movement.GearSpeedFactor, "Holding on costs speed");
            Assert.AreEqual(HealthConstants.MagbootSpeedFactor, movement.LocomotionPlaybackRate, 0.001f, "The gait slows with the body");

            Vector3 start = ragdoll.WindSamplePosition;
            ragdoll.ServerApplyWind(Vector3.right * 30f);
            yield return Hold(0.5f);
            Assert.AreEqual(BodyPresentationState.Locomotion, ragdoll.Presentation, "A violent gust cannot knock an anchored body down");
            Assert.That(Vector3.Distance(ragdoll.WindSamplePosition, start), Is.LessThan(0.01f), "Nor pull it");

            ragdoll.ServerKnockdown(1f);
            yield return WaitFor(() => ragdoll.IsKnockedDown, "something else floors the wearer");
            Assert.IsFalse(gear.MagbootsAnchored, "Boots on the floor hold nothing");
            Assert.AreEqual(1f, movement.GearSpeedFactor);
            start = ragdoll.WindSamplePosition;
            ragdoll.ServerApplyWind(Vector3.right * 30f);
            yield return Hold(0.5f);
            Assert.That(ragdoll.WindSamplePosition.x, Is.GreaterThan(start.x + 0.1f), "The wind takes a floored wearer");
            yield return WaitFor(() => !ragdoll.IsKnockedDown, "the wearer recovers", 10f);

            feet.RemoveItem(boots);
            yield return WaitFor(() => !gear.MagbootsOn, "taking the pair off turns them off");
            Assert.IsFalse(gear.MagbootsAvailable);
        }

        [UnityTest]
        public IEnumerator Magboots_DoNothingOverOpenSpace()
        {
            EvaGearController gear = Body.GetComponent<EvaGearController>();
            HumanoidBodyStateMachine bodyState = Body.GetComponent<HumanoidBodyStateMachine>();
            Worn(ContainerType.ShoeLeft).Dump();
            Wear(Magboots, ContainerType.ShoeLeft);
            yield return WaitFor(() => gear.MagbootsAvailable, "the pair offers magboots");

            gear.ServerSetMagboots(true);
            Assert.IsTrue(gear.MagbootsAnchored);
            bodyState.SetFloating(true);
            Assert.IsTrue(gear.MagbootsOn, "The switch stays where it is");
            Assert.IsFalse(gear.ServerAnchored(), "Nothing to hold on to");
            bodyState.SetFloating(false);
        }

        [UnityTest]
        public IEnumerator Boots_AreOneItemOnBothFeet()
        {
            return PairDressesBothSides(Magboots, ContainerType.ShoeLeft, ContainerType.ShoeRight, ClothType.ShoeLeft, ClothType.ShoeRight);
        }

        [UnityTest]
        public IEnumerator Gloves_AreOneItemOnBothHands()
        {
            return PairDressesBothSides(InsulatedGloves, ContainerType.GloveLeft, ContainerType.GloveRight, ClothType.GloveLeft, ClothType.GloveRight);
        }

        private IEnumerator PairDressesBothSides(string pair, ContainerType first, ContainerType second, ClothType firstCloth, ClothType secondCloth)
        {
            ClothedBodyPart[] parts = Body.GetComponentsInChildren<ClothedBodyPart>(true);
            GameObject firstPart = parts.Single(part => part.Type == firstCloth).gameObject;
            GameObject secondPart = parts.Single(part => part.Type == secondCloth).gameObject;
            AttachedContainer worn = Worn(first);
            worn.Dump();

            Item item = Wear(pair, first);
            SS3D.Systems.Inventory.Containers.Cloth cloth = item.GetComponent<SS3D.Systems.Inventory.Containers.Cloth>();
            yield return WaitFor(
                () => firstPart.GetComponent<SkinnedMeshRenderer>().sharedMesh == cloth.GetClothMesh(firstCloth)
                    && secondPart.GetComponent<SkinnedMeshRenderer>().sharedMesh == cloth.GetClothMesh(secondCloth),
                "one item dresses both sides");
            Assert.IsFalse(Worn(second).CanContainItem(item), "The second side takes nothing while the first is on the body");

            worn.RemoveItem(item);
            yield return WaitFor(() => !firstPart.activeSelf && !secondPart.activeSelf, "taking it off bares both sides");
        }

        [UnityTest]
        public IEnumerator CriticalBody_CannotWorkItsSwitches()
        {
            HumanHealthController health = Body.GetComponent<HumanHealthController>();
            EvaGearController gear = Body.GetComponent<EvaGearController>();
            Worn(ContainerType.Pocket).Dump();
            Worn(ContainerType.ShoeLeft).Dump();
            Wear(GasMask, ContainerType.Mask);
            Wear(EmergencyTank, ContainerType.Pocket);
            Wear(Magboots, ContainerType.ShoeLeft);
            yield return WaitFor(() => gear.InternalsAvailable && gear.MagbootsAvailable, "the gear offers both switches");

            gear.ClientToggleInternals();
            yield return WaitFor(() => gear.InternalsOn, "a healthy body works the switch");

            while (health.Snapshot.State != HealthState.Critical)
            {
                health.TickHealth(atmosphereO2: 0f);
            }

            Assert.IsTrue(health.Snapshot.IsConscious, "Critical is still conscious, so consciousness cannot be the gate");
            Assert.IsFalse(gear.CanSwitch);
            gear.ClientToggleInternals();
            gear.ClientToggleMagboots();
            yield return Hold(0.5f);
            Assert.IsTrue(gear.InternalsOn, "A critical body cannot switch internals off");
            Assert.IsFalse(gear.MagbootsOn, "Nor switch magboots on");
        }

        private AttachedContainer Worn(ContainerType slot) => Body.GetComponent<HumanInventory>().Containers.Single(worn => worn.ContainerType == slot);

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
