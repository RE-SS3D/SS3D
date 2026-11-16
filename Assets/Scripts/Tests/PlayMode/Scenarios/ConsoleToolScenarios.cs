using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Permissions;
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

            yield return Console.Expect("spawn NuclearAuthenticationDisk hand", "spawned in hand");
            Hand hand = Body.GetComponent<HumanInventory>().Hands.SelectedHand;
            Item disk = hand.ItemInHand;
            yield return Console.Expect("disk", ServerHelpers.LocalCkey);

            yield return Console.Expect("disk me", "Disk moved");
            yield return Hold(0.5f);
            Assert.IsNull(disk.Container, "The disk left the hand");
            Assert.IsTrue(hand.IsEmpty(), "The hand is empty");
            Assert.IsNull(disk.transform.parent, "The disk is back in the world");
            Assert.Less(Vector3.Distance(disk.transform.position, Body.transform.position), 1.5f, "The disk is at the body's feet");
            Assert.GreaterOrEqual(disk.transform.position.y, -0.05f, "The disk rests on the deck");
            Assert.IsTrue(disk.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), "The disk is visible");

            yield return Console.Expect("disk", "the floor");
            yield return Console.Expect("disk nosuchplayer", "no player with ckey");
            yield return Console.Expect($"disk {ServerHelpers.LocalCkey}", "Disk moved");
            yield return Console.Expect("disk one two", "too many arguments");

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
            yield return Console.Expect("ghostjump nosuchplayer", "no player with ckey");
            yield return Console.Expect("ghostjump one two", "too many arguments");
        }

        [UnityTest]
        public IEnumerator Nearest_TargetsACombatDummy_NotTheCaller()
        {
            yield return Console.Expect("spawndummy", "Combat dummy spawned");
            yield return Hold(0.5f);
            yield return Console.Expect("hurt nearest Chest 5 0", "Player hurt");
            StringAssert.DoesNotContain(ServerHelpers.LocalCkey, Console.LastAnswer, "nearest skips the caller");
            yield return Console.Expect("heal nearest", "Player fully healed");
        }

        [UnityTest]
        public IEnumerator BadArguments_AnswerWithErrorAndUsage()
        {
            yield return Console.Expect("hurt me Chest lots", "error: brute must be a number");
            StringAssert.Contains("usage: hurt <target> <zone> <brute> [burn=0]", Console.LastAnswer);
            yield return Console.Expect("hurt me Nowhere 5", "unknown zone");
            yield return Console.Expect("hurt", "missing <target>");
            yield return Console.Expect("hrut me Chest 5", "Did you mean:");
            StringAssert.Contains("hurt", Console.LastAnswer, "hurt is suggested");
            yield return Console.Expect("help hurt", "hurt <target> <zone> <brute> [burn=0]");
        }

        [UnityTest]
        public IEnumerator AdminCommands_AreRefusedForAUser()
        {
            PermissionSubSystem permissions = SubSystems.Get<PermissionSubSystem>();
            Assert.IsTrue(permissions.TryGetUserRole(ServerHelpers.LocalCkey, out ServerRoleTypes original), "The host has a role");
            permissions.ChangeUserPermission(ServerHelpers.LocalCkey, ServerRoleTypes.User);
            try
            {
                yield return Hold(0.2f);
                yield return Console.Expect("spawn MedicalPatch hand", "needs Administrator");
                yield return Console.Expect("accessgrant me Security", "needs Administrator");
            }
            finally
            {
                permissions.ChangeUserPermission(ServerHelpers.LocalCkey, original);
            }
        }

        [UnityTest]
        public IEnumerator Round_ReportsWhenAModeDoesNotApply()
        {
            // The fixture's round is running: no countdown to force, no end screen to hold.
            yield return Console.Expect("round force", "can't force: no countdown is running");
            yield return Console.Expect("round hold", "can't hold: the round isn't ending");
            yield return Console.Expect("round", "round needs one of");
        }
    }
}
