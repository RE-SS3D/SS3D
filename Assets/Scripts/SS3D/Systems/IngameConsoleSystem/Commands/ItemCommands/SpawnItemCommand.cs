using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using SS3D.Systems;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Systems.Inventory.Items;
using SS3D.Utils;
using SS3D.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;
using FishNet.Managing.Server;
using FishNet;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class SpawnItemCommand : Command
    {
        public override string LongDescription => "Spawn item using item name at the same position as human or at position x,z";
        public override string ShortDescription => "Spawn item";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override string Perform(string[] args)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;
            string itemName = args[0];
            Assets.TryGet((int)AssetDatabases.Items, (int)itemName.ToEnum<ItemId>(), out GameObject itemPrefab);

            ItemCommandUtilities.TryGetLocalPlayerEntity(out Entity entity);

            var itemInstance = Object.Instantiate(itemPrefab, entity.transform.position , Quaternion.identity);
            InstanceFinder.ServerManager.Spawn(itemInstance);

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
            var itemName = args[0];
            if(!Assets.TryGet((int) AssetDatabases.Items, (int) itemName.ToEnum<ItemId>(), out Item item))
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