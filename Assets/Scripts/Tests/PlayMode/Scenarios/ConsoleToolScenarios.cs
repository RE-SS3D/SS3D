using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Tile;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: test-server T3 disk teleport and #151 ghost jump through the real console path · Not cheaper because: both move live networked objects out of containers or past owner-driven movement, on a host with a body and a ghost
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.ConsoleTools)]
    public class ConsoleToolScenarios : ScenarioFixture
    {
        [UnityTest]
        public IEnumerator Disk_ListsHolder_TeleportsToFeet_AndCanBePickedUp()
        {
            foreach (NukeCard roomDisk in Object.FindObjectsByType<NukeCard>(FindObjectsSortMode.None))
                roomDisk.NetworkObject.Despawn();
            yield return Hold(0.3f);
            yield return Console.Expect("disk", "No authentication disk");

            yield return Console.Expect("spawninhand NuclearAuthenticationDisk", "spawned in hand");
            Hand hand = Body.GetComponent<HumanInventory>().Hands.SelectedHand;
            Item disk = hand.ItemInHand;
            yield return Console.Expect("disk", ServerHelpers.LocalCkey);

            yield return Console.Expect("disk here", "Disk moved");
            yield return Hold(0.5f);
            Assert.IsNull(disk.Container, "The disk left the hand");
            Assert.IsTrue(hand.IsEmpty(), "The hand is empty");
            Assert.IsNull(disk.transform.parent, "The disk is back in the world");
            Assert.Less(Vector3.Distance(disk.transform.position, Body.transform.position), 1.5f, "The disk is at the body's feet");
            Assert.GreaterOrEqual(disk.transform.position.y, -0.05f, "The disk rests on the deck");
            Assert.IsTrue(disk.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), "The disk is visible");

            yield return Console.Expect("disk", "the floor");
            yield return Console.Expect("disk nosuchplayer", "has no body");
            yield return Console.Expect($"disk {ServerHelpers.LocalCkey}", "Disk moved");
            yield return Console.Expect("disk one two", "Invalid");

            Assert.IsTrue(hand.Container.AddItem(disk), "The disk can be picked up again");
        }

        [UnityTest]
        public IEnumerator GhostJump_RefusedAlive_JumpsToStationAndStays()
        {
            yield return Console.Expect("ghostjump", "Only ghosts");
            Vector3 corpse = Body.transform.position;
            yield return Console.Expect($"kill {ServerHelpers.LocalCkey}", "Player killed");
            yield return WaitFor(() => HostEntity != null && HostEntity.GetComponent<HumanoidGhostController>() != null, "the player becomes a ghost");
            Entity ghost = HostEntity;
            yield return WaitFor(() => ghost.IsOwner && ghost.NetworkObject.ClientInitialized, "the host's client has the ghost");
            yield return Hold(0.5f);

            Teleport(ghost, corpse + new Vector3(40f, 3f, 40f));
            yield return Hold(0.5f);
            yield return Console.Expect("ghostjump", "Jumped to the station");
            yield return Hold(2f);
            Vector3 spawn = SubSystems.Get<TileSubSystem>().SpawnPoints.Records[0].Position;
            Vector3 offset = ghost.transform.position - spawn;
            Assert.Less(new Vector2(offset.x, offset.z).magnitude, 1f, "The ghost stays at the spawn point");

            yield return Console.Expect($"ghostjump {ServerHelpers.LocalCkey}", "already there");
            yield return Console.Expect("ghostjump nosuchplayer", "no body or ghost");
            yield return Console.Expect("ghostjump one two", "Invalid");
        }
    }
}
