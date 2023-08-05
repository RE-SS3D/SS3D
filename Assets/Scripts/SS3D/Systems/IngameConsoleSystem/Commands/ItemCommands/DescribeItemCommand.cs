using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Command to describe an item held in hand.
    /// </summary>
    public class DescribeItemCommand : Command
    {
        public override string LongDescription => "Describes the item in hand";
        public override string ShortDescription => "item.describe";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
		public override bool ServerCommand => false;

		public override string Perform(string[] args)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;

            Item item = ItemCommandUtilities.GetItemInHand();
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

