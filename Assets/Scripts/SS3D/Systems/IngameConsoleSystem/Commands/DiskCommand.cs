using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.PlayerControl;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Playtest wedge tool (admin-tools.md "teleport a key object"): find the nuke authentication
    /// disk, or pull it out of wherever it is and drop it at an admin's or a player's feet.
    /// </summary>
    public class DiskCommand : Command
    {
        private const string HereArg = "here";

        public override string ShortDescription => "Find or teleport the authentication disk";

        public override string LongDescription =>
            "With no arguments, lists every authentication disk and who or what holds it. " +
            "\"disk here\" moves it to your feet; \"disk (ckey)\" moves it to that player's feet. " +
            "A disk held in a hand or bag is taken out first.";

        public override string Usage => "[here | (ckey)]";

        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;

        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(string Mode) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            List<NukeCard> disks = FindDisks();
            if (disks.Count == 0)
            {
                return "No authentication disk exists this round.";
            }

            if (values.Mode == null)
            {
                return Describe(disks);
            }

            EntitySubSystem entities = SubSystems.Get<EntitySubSystem>();
            Entity destination;
            if (values.Mode == HereArg)
            {
                if (!entities.TryGetOwnedEntity(conn, out destination) || destination == null)
                {
                    return "You have no body to teleport the disk to. Use \"disk (ckey)\".";
                }
            }
            else
            {
                Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(values.Mode);
                destination = player != null ? entities.GetSpawnedEntity(player) : null;
                if (destination == null)
                {
                    return $"Player {values.Mode} has no body to teleport the disk to.";
                }
            }

            // Several disks only happen on a broken map; move the first and say so.
            NukeCard disk = disks[0];
            string from = DescribeLocation(disk);
            if (!TryTeleport(disk, destination.transform.position, out string failure))
            {
                return failure;
            }

            string admin = SubSystems.Get<PlayerSubSystem>().GetCkey(conn);
            Log.Information(this, "Admin {admin} teleported the authentication disk from {from} to {destination}",
                Logs.ServerOnly, admin, from, destination.name);

            string answer = $"Disk moved from {from} to {destination.name} at {destination.transform.position}.";
            return disks.Count > 1 ? answer + $" {disks.Count - 1} other disk(s) left in place." : answer;
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length == 0)
            {
                return response.MakeValid(new CalculatedValues(null));
            }

            if (args.Length != 1)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            string mode = args[0].Equals(HereArg, System.StringComparison.OrdinalIgnoreCase) ? HereArg : args[0];
            return response.MakeValid(new CalculatedValues(mode));
        }

        private static List<NukeCard> FindDisks()
        {
            List<NukeCard> disks = new();
            foreach (NukeCard disk in UnityEngine.Object.FindObjectsByType<NukeCard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Prefab assets and despawned pool copies have no live NetworkObject on the server.
                if (disk != null && disk.IsSpawned)
                {
                    disks.Add(disk);
                }
            }

            return disks;
        }

        private static bool TryTeleport(Item disk, Vector3 position, out string failure)
        {
            failure = null;
            AttachedContainer container = disk.Container;
            if (container != null)
            {
                container.RemoveItem(disk);
                if (disk.Container != null)
                {
                    failure = $"Disk is locked in {container.gameObject.name} and can't be taken out.";
                    return false;
                }
            }

            disk.GiveOwnership(null);
            ItemUtility.Place(disk, position, disk.transform.rotation);
            return true;
        }

        private static string Describe(List<NukeCard> disks)
        {
            StringBuilder builder = new();
            for (int i = 0; i < disks.Count; i++)
            {
                NukeCard disk = disks[i];
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append($"Disk {i + 1}: {DescribeLocation(disk)}, at {disk.transform.position}");
            }

            return builder.ToString();
        }

        private static string DescribeLocation(Item disk)
        {
            AttachedContainer container = disk.Container;
            if (container == null)
            {
                return "the floor";
            }

            Entity holder = container.GetComponentInParent<Entity>();
            string holderKey = holder != null ? HolderCkey(holder) : null;
            if (holder == null)
            {
                return container.gameObject.name;
            }

            return string.IsNullOrEmpty(holderKey)
                ? $"{container.gameObject.name} on {holder.name}"
                : $"{container.gameObject.name} on {holder.name} ({holderKey})";
        }

        private static string HolderCkey(Entity holder)
        {
            Mind mind = holder.Mind;
            return mind != null && mind.player != null ? mind.player.Ckey : null;
        }
    }
}
