using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Systems.Area;
using SS3D.Systems.Combat;
using SS3D.Systems.Electricity;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using SS3D.UI.MachineInterface;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a shot-up or blown-off APC leaves its room on the fixtures' own emergency cells, then dark · Not cheaper because: the shots run the server fire path into the live APC, and the fixtures read their area's APC through the area registry and electricity tick
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Combat)]
    public class PowerLossScenarios : ScenarioFixture
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        /// <summary>Short fixture cells so the run-down fits a scenario.</summary>
        private const float CellSeconds = 2f;

        [UnityTest]
        public IEnumerator ShotUpApc_LightsRunOnTheirCells_ThenGoDark()
        {
            ChangesRoom = true;
            ApcController apc = Object.FindObjectsByType<ApcController>(FindObjectsSortMode.None).First();
            LightPower[] lights = LightsOf(apc);
            Assert.IsNotEmpty(lights, "The APC's room has light fixtures");
            yield return ShortenCells(lights);

            BreakableDevice casing = apc.GetComponent<BreakableDevice>();
            Vector3 facing = apc.transform.forward;
            Vector3 stand = apc.transform.position + (facing * 2.5f);
            Teleport(Body, new Vector3(stand.x, Body.transform.position.y, stand.z));
            Body.transform.rotation = Quaternion.LookRotation(-facing);
            yield return Hold(0.4f);
            Physics.SyncTransforms();

            EmptyHands(Body);
            Hands hands = Body.GetComponent<Hands>();
            Hand hand = TwoHandedWeaponRules.FindHand(hands, HandSide.Right);
            hands.ServerSelectHand(hand);
            Assert.IsTrue(hand.Container.AddItem(SubSystems.Get<ItemSubSystem>().SpawnItem(Items.M4, Body.transform.position, Quaternion.identity)));
            Vector3 panel = apc.GetComponent<Collider>().bounds.center;
            for (int shot = 0; shot < 10 && !casing.IsBroken; shot++)
            {
                yield return Fire(panel);
            }

            Assert.IsTrue(casing.IsBroken, "Rifle rounds break the APC");
            Assert.AreEqual(ApcControlFlags.None, apc.Channels, "A broken APC feeds nothing");
            Assert.IsTrue(apc != null, "It stays on the wall");

            yield return WaitFor(() => lights.All(light => light.Visual == LightPower.FixtureVisual.Emergency && light.IsOnEmergencyCell),
                "the fixtures fall back to their own cells", 3f);
            yield return WaitFor(() => lights.All(light => light.Visual == LightPower.FixtureVisual.Off),
                "the cells run down and the room goes dark", CellSeconds + 3f);
        }

        [UnityTest]
        public IEnumerator BlownWall_TakesTheApc_LightsRunOnTheirCells()
        {
            ChangesRoom = true;
            ApcController apc = Object.FindObjectsByType<ApcController>(FindObjectsSortMode.None).First();
            LightPower[] lights = LightsOf(apc);
            yield return ShortenCells(lights);

            PlacedTileObject mount = apc.GetComponent<PlacedTileObject>();
            TileCoord coord = new(SubSystems.Get<TileSubSystem>().CurrentMap.MapId, mount.WorldOrigin.x, mount.WorldOrigin.y);
            Assert.IsTrue(SubSystems.Get<StructuralDamageSubSystem>().TryApplyStructuralDamage(coord, 1000f, StructuralDamageSource.Blast),
                "The blast takes the APC's wall");
            yield return WaitFor(() => apc == null, "the APC goes with its wall");

            yield return WaitFor(() => lights.All(light => light.Visual == LightPower.FixtureVisual.Emergency),
                "the fixtures fall back to their own cells", 3f);
            yield return WaitFor(() => lights.All(light => light.Visual == LightPower.FixtureVisual.Off),
                "the cells run down and the room goes dark", CellSeconds + 3f);
        }

        private static LightPower[] LightsOf(ApcController apc)
        {
            AreaSubSystem areas = SubSystems.Get<AreaSubSystem>();
            Assert.IsTrue(areas.TryResolveAreaIdForDevice(apc.GetComponent<PlacedTileObject>(), out AreaId apcArea));
            return Object.FindObjectsByType<LightPower>(FindObjectsSortMode.None)
                .Where(light => areas.TryResolveAreaIdForDevice(light.GetComponent<PlacedTileObject>(), out AreaId id) && id == apcArea)
                .ToArray();
        }

        /// <summary>Swaps in short cells and waits until they've armed on mains.</summary>
        private static IEnumerator ShortenCells(LightPower[] lights)
        {
            foreach (LightPower light in lights)
            {
                typeof(LightPower).GetField("_cellRunSeconds", Private).SetValue(light, CellSeconds);
                typeof(LightPower).GetField("_cell", Private).SetValue(light, null);
            }

            yield return WaitFor(() => lights.All(light => light.Visual == LightPower.FixtureVisual.Normal && light.EmergencyCell.Armed),
                "the room is lit from mains and the fixture cells are armed", 10f);
        }

        /// <summary>Harm intent, a camera ray at <paramref name="target"/>, and the server fire path the trigger reaches.</summary>
        private IEnumerator Fire(Vector3 target)
        {
            InteractionController controller = Body.GetComponent<InteractionController>();
            typeof(InteractionController).GetField("_ownerIntent", Private).SetValue(controller, IntentType.Harm);

            CombatInteractionNetwork combat = Body.GetComponent<CombatInteractionNetwork>();
            Vector3 eye = Body.transform.position + (Vector3.up * 1.6f);
            typeof(CombatInteractionNetwork).GetField("_meleeAimRayOrigin", Private).SetValue(combat, eye);
            typeof(CombatInteractionNetwork).GetField("_meleeAimPoint", Private).SetValue(combat, target);
            typeof(CombatInteractionNetwork).GetField("_hasMeleeAimRay", Private).SetValue(combat, true);
            typeof(CombatInteractionNetwork).GetMethod("CmdRunRangedFire", Private).Invoke(combat, null);
            yield return Hold(0.6f);
        }
    }
}
