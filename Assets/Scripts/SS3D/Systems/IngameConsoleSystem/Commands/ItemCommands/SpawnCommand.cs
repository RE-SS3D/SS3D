using System.Collections.Generic;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands.ItemCommands
{
    /// <summary>Spawns an item at the caller's feet or into their selected hand; scenarios use <c>hand</c> to arm a body.</summary>
    public class SpawnCommand : Command
    {
        private const string Hand = "hand";

        private static readonly IReadOnlyList<CommandParameter> Params = new[]
        {
            P.Item(),
            P.Choice("at", "feet", Hand).Default("feet"),
        };

        public override string Name => "spawn";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Spawn an item by name or asset id at your feet or in your empty selected hand; a two-handed rifle needs both hands free";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Params;
        public override string Example => "MedicalPatch hand";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            Entity entity = context.CallerEntity;
            if (entity == null)
            {
                return CommandResult.Error("you have no body to spawn next to");
            }

            CommandItem item = args.Item();
            ItemSubSystem items = SubSystems.Get<ItemSubSystem>();

            if (args.Text("at") == Hand)
            {
                HumanInventory inventory = entity.GetComponentInParent<HumanInventory>();
                Hands hands = inventory != null ? inventory.Hands : null;
                Hand selected = hands != null ? hands.SelectedHand : null;
                if (selected == null)
                {
                    return CommandResult.Error("you have no hands");
                }

                // A two-handed rifle only fits its grip hand, so the selected hand would refuse it and the
                // item would stay where it spawned, at the origin. Route it the way a pickup does.
                if (!TwoHandedWeaponRules.TryResolveHandForItem(hands, item.Prefab, selected, out Hand hand))
                {
                    return CommandResult.Error(TwoHandedWeaponRules.RequiresBothHands(item.Prefab, out _)
                        ? $"{item.Prefab.Name} needs both hands free"
                        : "your selected hand is not empty");
                }

                Item spawned = items.SpawnItemInContainer(item.Prefab.GameObject, hand.Container);
                return spawned != null
                    ? CommandResult.Success($"item {spawned.Name} spawned in hand")
                    : CommandResult.Error($"{item.Prefab.Name} does not fit in your hand");
            }

            Vector3 position = entity.transform.position;
            Quaternion rotation = item.Prefab.GetWorldFacing(entity.transform.eulerAngles.y);
            items.SpawnItem(item.AssetId, position, rotation);
            return CommandResult.Success($"item {item.Prefab.Name} spawned at {position}");
        }
    }
}
