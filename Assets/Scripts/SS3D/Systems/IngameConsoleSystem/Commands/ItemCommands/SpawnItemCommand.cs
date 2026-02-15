using FishNet.Connection;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands.ItemCommands
{
    public class SpawnItemCommand : Command
    {
        public override string LongDescription => "Spawn item using item name at the same position as human or at position x,z";
        public override string ShortDescription => "Spawn item";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;

        public override CommandType Type => CommandType.Server;

        public override string Perform(string[] args, NetworkConnection conn)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);

            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;

            string itemName = args[0];

            if (!SubSystems.Get<EntitySubSystem>().TryGetOwnedEntity(conn, out Entity entity))
            {
                return "Connection does not own any entity registered in entity system.";
            }

            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            itemSystem.CmdSpawnItem(itemName, entity.transform.position, Quaternion.identity);

            return $"item {itemName} spawned at position {entity.transform.position}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new CheckArgsResponse();

            if (args.Length != 1 && args.Length != 3)
            {
                response.IsValid = false;
                response.InvalidArgs = "Invalid number of arguments";

                return response;
            }

            string itemName = args[0];

            if (!Assets.TryGet(AssetDatabases.Items, itemName, out Item item))
            {
                response.IsValid = false;
                response.InvalidArgs = $"item with name {itemName} not found";

                return response;
            }

            response.IsValid = true;

            return response;
        }
    }
}