using FishNet.Connection;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
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

            if (!TryResolveItem(itemName, out Item itemPrefab, out string assetId))
            {
                return $"item with name {itemName} not found";
            }

            Quaternion rotation = itemPrefab.GetWorldFacing(entity.transform.eulerAngles.y);
            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            itemSystem.CmdSpawnItem(assetId, entity.transform.position, rotation);

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

            if (!TryResolveItem(args[0], out _, out _))
            {
                response.IsValid = false;
                response.InvalidArgs = $"item with name {args[0]} not found";

                return response;
            }

            response.IsValid = true;

            return response;
        }

        /// <summary>
        /// Resolve by asset GUID (Items.* constants) or by prefab/object name. Assets.Get would log an error for a name, so only TryGet is used.
        /// </summary>
        internal static bool TryResolveItem(string itemNameOrId, out Item item, out string assetId)
        {
            if (Assets.TryGet(AssetDatabases.Items, itemNameOrId, out item) && item != null)
            {
                assetId = itemNameOrId;
                return true;
            }

            AssetDatabase database = Assets.GetDatabase(AssetDatabases.Items);
            if (database?.Assets == null)
            {
                item = null;
                assetId = null;
                return false;
            }

            foreach (var pair in database.Assets)
            {
                Item candidate = null;
                if (pair.Value is Item direct)
                {
                    candidate = direct;
                }
                else if (pair.Value is GameObject go)
                {
                    candidate = go.GetComponent<Item>();
                }

                if (candidate == null)
                {
                    continue;
                }

                if (!string.Equals(candidate.Name, itemNameOrId, System.StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(candidate.gameObject.name, itemNameOrId, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                item = candidate;
                assetId = pair.Key;
                return true;
            }

            item = null;
            assetId = null;
            return false;
        }
    }
}
