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
        public override string LongDescription => "Describes the item in hand";
        public override string ShortDescription => "item.describe";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;

            Item item = ItemCommandUtilities.GetItemInHand(conn);
            if (item == null)
            {
                return "No item in hand";
            }
            return item.Describe();
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new CheckArgsResponse();
            if (args.Length != 0)
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

