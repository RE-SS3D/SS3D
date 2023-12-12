using FishNet.Connection;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands.ItemCommands
{
    public class SpawnItemCommand : Command
    {
        public override string LongDescription => "Spawn item using item name at the same position as human";
        public override string Usage => "(item name)";
        public override string ShortDescription => "Spawn item";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Server;
        
        private record CalculatedValues(string ItemName) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;
            
            if(!Subsystems.Get<EntitySystem>().TryGetOwnedEntity(conn, out Entity entity))
                return "Connection does not own any entity registered in entity system.";
            
            ItemSystem itemSystem = Subsystems.Get<ItemSystem>();
            itemSystem.CmdSpawnItem(values.ItemName.ToEnum<ItemId>(), entity.transform.position, Quaternion.identity);
            return $"item {values.ItemName} spawned at position {entity.transform.position}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length != 1 && args.Length != 3) return response.MakeInvalid("Invalid number of arguments");
            
            string itemName = args[0];
            if(!Assets.TryGet((int) AssetDatabases.Items, (int) itemName.ToEnum<ItemId>(), out Item _))
                return response.MakeInvalid( $"item with name {itemName} not found");
            
            return response.MakeValid(new CalculatedValues(itemName));
        }
    }
}