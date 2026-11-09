using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Electricity;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a ballistic round through a fixture's bulb breaks it and kills the light, and the bulb can be taken out and replaced by hand · Not cheaper because: the shot runs the server fire path against the live fixture's glass, and the light's lit mode comes from the room's APC and area lighting
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Combat)]
    public class LightBulbScenarios : ScenarioFixture
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator ShotBulb_GoesDark_ThenIsReplacedByHand()
        {
            LightFixtureBulb bulb = Object.FindObjectsByType<LightFixtureBulb>(FindObjectsSortMode.None).FirstOrDefault();
            Assert.IsNotNull(bulb, "The test room has a light fixture");
            LightPower power = bulb.GetComponent<LightPower>();
            ChangesRoom = true;
            yield return WaitFor(() => power.Visual != LightPower.FixtureVisual.Off, "the room's APC lights the fixture", 10f);

            Assert.IsTrue(bulb.TryGetGlassBounds(out Bounds glass));
            Vector3 facing = bulb.transform.forward;
            Vector3 stand = bulb.transform.position + (facing * 2.5f);
            Teleport(Body, new Vector3(stand.x, Body.transform.position.y, stand.z));
            Body.transform.rotation = Quaternion.LookRotation(-facing);
            yield return Hold(0.4f);
            Physics.SyncTransforms();

            EmptyHands(Body);
            Hands hands = Body.GetComponent<Hands>();
            Hand hand = TwoHandedWeaponRules.FindHand(hands, HandSide.Right);
            hands.ServerSelectHand(hand);
            ItemSubSystem items = SubSystems.Get<ItemSubSystem>();
            Assert.IsTrue(hand.Container.AddItem(items.SpawnItem(Items.M4, Body.transform.position, Quaternion.identity)), "The rifle goes in the hand");
            // The accuracy cone is random; a couple of rounds at two metres are plenty to hit 15 cm of glass.
            for (int shot = 0; shot < 3 && bulb.State == LightBulbState.Intact; shot++)
            {
                yield return Fire(glass.center);
            }

            Assert.AreEqual(LightBulbState.Broken, bulb.State, "The round breaks the bulb");
            yield return WaitFor(() => power.Visual == LightPower.FixtureVisual.Off, "a broken bulb gives no light");
            Assert.IsFalse(bulb.TryGetGlassBounds(out _), "Broken glass can't be shot again");

            EmptyHands(Body);
            yield return Hold(0.2f);
            Teleport(Body, new Vector3(bulb.transform.position.x + (facing.x * 0.8f), Body.transform.position.y,
                bulb.transform.position.z + (facing.z * 0.8f)));
            yield return Hold(0.4f);
            Physics.SyncTransforms();

            RemoveLightBulbInteraction remove = new();
            InteractionEvent take = new(hand, bulb);
            Assert.IsTrue(remove.CanInteract(take), "An empty hand can take the broken bulb out");
            remove.Start(take, null);
            Assert.AreEqual(LightBulbState.Empty, bulb.State);
            Assert.IsTrue(hand.ItemInHand != null && hand.ItemInHand.GetComponent<LightBulbItem>().IsBroken,
                "The broken bulb comes out into the hand");

            // Let the hand's add reach the client before the item goes; despawning in the same tick sends it as a prefab.
            yield return Hold(0.2f);
            EmptyHands(Body);
            yield return Hold(0.2f);
            Item spare = items.SpawnItem(SpareFor(bulb), Body.transform.position, Quaternion.identity);
            Assert.IsTrue(hand.Container.AddItem(spare), "A spare goes in the hand");
            yield return Hold(0.2f);
            IInteractionSource tool = hand.GetActiveTool();
            InsertLightBulbInteraction insert = new();
            InteractionEvent fit = new(tool, bulb);
            Assert.IsTrue(insert.CanInteract(fit), "A spare of the right kind fits the empty socket");
            insert.Start(fit, null);
            Assert.AreEqual(LightBulbState.Intact, bulb.State);
            yield return WaitFor(() => power.Visual != LightPower.FixtureVisual.Off, "the new bulb lights");
            yield return WaitFor(() => hand.IsEmpty(), "the spare is used up");

            // Shot again and replaced straight away: the broken glass drops out onto the floor.
            Assert.IsTrue(bulb.ServerBreak());
            int brokenOnFloor = CountLooseBroken();
            spare = items.SpawnItem(SpareFor(bulb), Body.transform.position, Quaternion.identity);
            Assert.IsTrue(hand.Container.AddItem(spare));
            yield return Hold(0.2f);
            fit = new InteractionEvent(hand.GetActiveTool(), bulb);
            Assert.AreEqual(bulb.Kind == LightBulbKind.Tube ? "Replace light tube" : "Replace light bulb", insert.GetName(fit));
            insert.Start(fit, null);
            Assert.AreEqual(LightBulbState.Intact, bulb.State);
            yield return WaitFor(() => CountLooseBroken() == brokenOnFloor + 1, "the swapped-out broken bulb drops");
        }

        private static string SpareFor(LightFixtureBulb bulb) =>
            ((SS3D.Data.AssetDatabases.ObjectAssetReference)typeof(LightFixtureBulb).GetField("_intactItem", Private).GetValue(bulb)).Id;

        private static int CountLooseBroken() => Object.FindObjectsByType<LightBulbItem>(FindObjectsSortMode.None)
            .Count(item => item.IsBroken && !item.GetComponent<Item>().IsInContainer());

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
