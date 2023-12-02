using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Command to get all traits in console on an item held in hand.
    /// </summary>
    public class GetTraitCommand : Command
    {
        public override string ShortDescription => "Get all traits from item in hand";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        
        private record CalculatedValues : ICalculatedValues;
        
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            Item item = ItemCommandUtilities.GetItemInHand(conn);
            if (item == null) return response.MakeInvalid("No item in hand").InvalidArgs;
            
            if (item.Traits.Count == 0) return response.MakeInvalid("Item in hand has no traits").InvalidArgs;

            string debugString = "Item " + item.Name + " has traits: ";
            for (int i = 0; i < item.Traits.Count; i++)
            {
                if (i == item.Traits.Count - 1)
                {
                    debugString += item.Traits[i].Name;
                }
                else
                {
                    debugString += item.Traits[i].Name + ", ";
                }
            }

            return debugString;
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length != 0) return response.MakeInvalid("Invalid number of arguments");

            return response.MakeValid(new CalculatedValues());
        }
    }
}
