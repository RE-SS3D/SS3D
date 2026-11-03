using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.IngameConsoleSystem.Commands.ItemCommands
{
    /// <summary>Spawns an item straight into the caller's selected hand; scenarios use it to arm a body.</summary>
    public class SpawnInHandCommand : Command
    {
        public override string ShortDescription => "Spawn item in your selected hand";
        public override string LongDescription => "Spawn an item by name or asset id into the selected hand, which must be empty";
        public override string Usage => "(item)\nexample: spawninhand MedicalPatch";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            CheckArgsResponse response = CheckArgs(args);
            if (!response.IsValid)
                return response.InvalidArgs;

            if (!SubSystems.Get<EntitySubSystem>().TryGetSpawnedEntity(conn, out Entity entity))
                return "Connection does not own any entity registered in entity system.";

            HumanInventory inventory = entity.GetComponentInParent<HumanInventory>();
            Hand hand = inventory != null ? inventory.Hands.SelectedHand : null;
            if (hand == null)
                return "This entity has no hands.";

            if (!hand.IsEmpty())
                return "Your selected hand is not empty.";

            SpawnItemCommand.TryResolveItem(args[0], out Item prefab, out _);
            Item item = SubSystems.Get<ItemSubSystem>().SpawnItemInContainer(prefab.GameObject, hand.Container);
            return $"item {item.Name} spawned in hand";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length != 1)
                return response.MakeInvalid("Invalid number of arguments");

            return SpawnItemCommand.TryResolveItem(args[0], out _, out _)
                ? response.MakeValid(null)
                : response.MakeInvalid($"item with name {args[0]} not found");
        }
    }
}
