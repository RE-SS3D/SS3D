using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.IngameConsoleSystem.Commands.ItemCommands
{
    /// <summary>
    /// Command to describe an item held in the active hand of the player calling the command.
    /// </summary>
    public class DescribeItemCommand : Command
    {
        public override string ShortDescription => "Describe the item in hand";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        
        private record CalculatedValues : ICalculatedValues;
        
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            Item item = ItemCommandUtilities.GetItemInHand(conn);
            if (item == null)
            {
                return response.MakeInvalid("No item in hand").InvalidArgs;
            }
            return item.Describe();
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length != 0) return response.MakeInvalid("Invalid number of arguments");
            
            return response.MakeValid(new CalculatedValues());
        }
    }
}

