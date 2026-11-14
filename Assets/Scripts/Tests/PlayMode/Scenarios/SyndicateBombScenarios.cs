using System.Collections;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Systems.Entities;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: the Syndicate bomb through real interactions (explosives-destruction.md §6): Use (E) on the held beacon calls it down,
    // nobody can lift it, it drags only while loose, the timer never drops below 90 s, armed bolts lock, the nuke's
    // screwdriver-then-wirecutters defuse, and its blast holes the hull · Not cheaper because: the interaction
    // pipeline, Draggable, item spawn and blast resolution only meet on a live host with a body
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Explosives)]
    public class SyndicateBombScenarios : ScenarioFixture
    {
        private const string Bomb = "4f33f4e24e81db98f94f16cd4bfe04c1";
        private const string Beacon = "3f4cb34ca7d313d9497131616ada230b";

        private static readonly Vector3 RoomCentre = new(0f, 0f, -1f);

        private static TileSubSystem Tiles => SubSystems.Get<TileSubSystem>();

        private static ItemSubSystem ItemSpawner => SubSystems.Get<ItemSubSystem>();

        [UnityTest]
        public IEnumerator Beacon_DragBoltArmDefuse()
        {
            ChangesRoom = true;
            Teleport(Body, new Vector3(RoomCentre.x, Body.transform.position.y, RoomCentre.z));
            EmptyHands(Body);
            Hand hand = Body.GetComponent<Hands>().SelectedHand;
            yield return Hold(0.3f);

            Item beacon = ItemSpawner.SpawnItem(Beacon, Body.transform.position, Quaternion.identity);
            Assert.IsTrue(hand.Container.AddItem(beacon), "The beacon goes in the active hand");
            yield return Hold(0.3f);
            // Through the Use key's own path (E), which used to pass the body root and silently do nothing.
            Body.GetComponent<InteractionController>().UseHeldItem();
            SyndicateBomb bomb = null;
            yield return WaitFor(() => (bomb = Object.FindFirstObjectByType<SyndicateBomb>()) != null, "the bomb arrives", 3f);
            Assert.IsTrue(beacon == null || !beacon.IsSpawned, "The beacon is used up");
            Assert.IsNull(hand.ItemInHand);
            yield return Hold(1f);

            Item bombItem = bomb.GetComponent<Item>();
            Vector3 offset = bomb.transform.position - Body.transform.position;
            Assert.Less(new Vector2(offset.x, offset.z).magnitude, 1.5f, "It lands at the user's feet");
            Assert.IsFalse(hand.Container.CanContainItem(bombItem), "Too heavy to lift");
            Assert.IsNotNull(Run(bomb.gameObject, "Pickup"), "No pick up offered");

            Assert.IsNotNull(Run(bomb.gameObject, "SyndicateBombTimerDown"), "Already at the 90 s floor");
            Assert.IsNull(Run(bomb.gameObject, "SyndicateBombTimerUp"));
            yield return WaitFor(() => Mathf.Approximately(bomb.State.TimerSeconds, 120f), "the timer goes up to 2:00", 2f);

            Draggable draggable = bomb.GetComponent<Draggable>();
            Assert.IsNull(Run(bomb.gameObject, "Drag"), "A loose bomb drags");
            yield return WaitFor(() => draggable.Dragged, "the drag starts", 2f);
            Assert.IsNull(Run(bomb.gameObject, "Drag"), "and lets go");
            yield return WaitFor(() => !draggable.Dragged, "the drag ends", 2f);

            Assert.IsNull(Run(bomb.gameObject, "SyndicateBombArm"), "An empty hand arms it");
            yield return WaitFor(() => bomb.State.Armed, "the bomb arms", SyndicateBomb.ArmSeconds + 2f);
            Assert.Greater(bomb.Remaining, 110f, "The set timer runs");

            Item wrench = Give(hand, Data.Generated.Items.Wrench);
            yield return Hold(0.3f);
            Assert.IsNull(Run(bomb.gameObject, "SyndicateBombWrench"), "The wrench bolts it down while armed");
            yield return WaitFor(() => bomb.State.Anchored, "the bomb is bolted", SyndicateBomb.WrenchSeconds + 2f);
            Assert.IsTrue(bombItem.GetComponent<Rigidbody>().isKinematic, "Bolted down, the body is fixed");
            Assert.IsNotNull(Run(bomb.gameObject, "SyndicateBombWrench"), "Armed bolts are locked");
            wrench.Delete();
            yield return Hold(0.3f);
            Assert.IsNotNull(Run(bomb.gameObject, "Drag"), "A bolted bomb can't be dragged");

            Item screwdriver = Give(hand, Data.Generated.Items.ScrewdriverBlue);
            yield return Hold(0.3f);
            Assert.IsNull(Run(bomb.gameObject, "SyndicateBombOpenPanel"));
            yield return WaitFor(() => bomb.State.PanelOpen, "the panel opens", SyndicateBomb.OpenPanelSeconds + 2f);
            screwdriver.Delete();

            Item cutters = Give(hand, Data.Generated.Items.Wirecutters);
            yield return Hold(0.3f);
            Assert.IsNull(Run(bomb.gameObject, "SyndicateBombCutWires"));
            yield return WaitFor(() => !bomb.State.Armed, "the bomb is defused", SyndicateBomb.CutWiresSeconds + 2f);
            cutters.Delete();

            wrench = Give(hand, Data.Generated.Items.Wrench);
            yield return Hold(0.3f);
            Assert.IsNull(Run(bomb.gameObject, "SyndicateBombWrench"), "Defused, the bolts come off again");
            yield return WaitFor(() => !bomb.State.Anchored, "the bomb is unbolted", SyndicateBomb.WrenchSeconds + 2f);
            wrench.Delete();
            bombItem.Delete();
        }

        // Guards: the bomb's blast is the largest in the game and opens the hull under it
        [UnityTest]
        public IEnumerator Detonation_OpensTheHull_AndConsumesTheBomb()
        {
            ChangesRoom = true;
            EntityInvincibility.SetServer(Body.Owner.ClientId, true);
            try
            {
                Item bomb = ItemSpawner.SpawnItemAtRest(Bomb, RoomCentre + Vector3.up * 0.2f);
                yield return Hold(0.5f);
                TileCoord epicenter = Tiles.QueryService.WorldToTile(bomb.transform.position, Tiles.CurrentMap.MapId);

                bomb.GetComponent<SyndicateBomb>().Detonate();
                yield return Hold(0.5f);

                Assert.IsTrue(bomb == null || !bomb.IsSpawned, "The bomb is gone");
                Assert.IsTrue(Tiles.QueryService.TryGetOccupancy(epicenter, out TileOccupancy occupancy) && !occupancy.HasPlenum,
                    "One bomb opens the hull beneath it");
            }
            finally
            {
                EntityInvincibility.ClearServer(Body.Owner.ClientId);
            }
        }

        private static Item Give(Hand hand, string id)
        {
            Item item = ItemSpawner.SpawnItem(id, hand.transform.position, Quaternion.identity);
            Assert.IsTrue(hand.Container.AddItem(item), $"{item.name} goes in the active hand");
            return item;
        }

        private string Run(GameObject target, string genericName) =>
            Body.GetComponent<InteractionController>()
                .RunInteractionForTesting(target, target.transform.position, genericName, IntentType.Help);
    }
}
