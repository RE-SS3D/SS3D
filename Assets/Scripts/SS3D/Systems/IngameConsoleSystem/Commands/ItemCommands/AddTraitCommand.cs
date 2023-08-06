using SS3D.Systems.Inventory.Items;
using UnityEngine;
using SS3D.Systems.Permissions;
using FishNet.Object;
using FishNet.Connection;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Command to add a new trait with a chosen name on an item held in hand.
    /// </summary>
    public class AddTraitCommand : Command
    {
        public override string LongDescription => "Adds a trait to the item in hand";
        public override string ShortDescription => "item.addtrait";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override bool ServerCommand => true;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;
            string traitName = args[0];

            Item item = ItemCommandUtilities.GetItemInHand(conn);
            if (item == null)
            {
                return "No item in hand";
            }

            Trait trait = (Trait)ScriptableObject.CreateInstance("Trait");
            trait.Name = traitName;
            item.AddTrait(trait);

           return "Trait " + traitName + " added to Item " + item.Name;
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new CheckArgsResponse();
            if (args.Length != 1)
            {
                response.IsValid = false;
                response.InvalidArgs = "Invalid number of arguments";
                return response;
            }

            response.IsValid = true;
            return response;
        }
    }
}