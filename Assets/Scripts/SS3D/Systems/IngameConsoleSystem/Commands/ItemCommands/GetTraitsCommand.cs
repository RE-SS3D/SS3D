using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class GetTraitCommand : Command
    {
        public override string LongDescription => "Get all traits from item in hand";
        public override string ShortDescription => "item.traits";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;

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
            if (item.Traits.Count == 0)
            {
                return "Item in hand has no traits";
            }

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
