using SS3D.Systems.Inventory.Items;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using SS3D.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Command to add a new trait with a chosen name on an item held in hand.
    /// </summary>
    public class AddTraitCommand : Command
    {
        public override string ShortDescription => "Adds a trait to the item in hand";
        public override string Usage => "(trait name)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        
        private record CalculatedValues : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;
            
            string traitName = args[0];
            Item item = ItemCommandUtilities.GetItemInHand(conn);
            if (item == null) return response.MakeInvalid("No item in hand").InvalidArgs;
            
            Trait trait = ScriptableObject.CreateInstance<Trait>();
            trait.Name = traitName;
            item.AddTrait(trait);

           return "Trait " + traitName + " added to Item " + item.Name;
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length != 1) return response.MakeInvalid("Invalid number of arguments");
            
            return response.MakeValid(new CalculatedValues());
        }
    }
}