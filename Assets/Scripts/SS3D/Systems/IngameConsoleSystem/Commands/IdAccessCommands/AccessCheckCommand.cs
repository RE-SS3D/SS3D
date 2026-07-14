using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.IdAccess;

namespace SS3D.Systems.IngameConsoleSystem.Commands.IdAccessCommands
{
    public class AccessCheckCommand : Command
    {
        public override string ShortDescription => "Show on-person ID credential state";
        public override string LongDescription =>
            "Lists every ID card on the target player, which one the access resolver picks, "
            + "the bound crew record's access mask, and optionally tests a required level.";
        public override string Usage => "[ckey] [level]";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(string Ckey, AccessLevel? TestLevel) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            if (!IdAccessCommandUtilities.TryResolveTarget(conn, values.Ckey, out var inventory, out string error))
            {
                return error;
            }

            return IdAccessCommandUtilities.BuildCredentialReport(inventory, values.TestLevel);
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length > 2)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            string ckey = args.Length >= 1 ? args[0] : null;
            AccessLevel? testLevel = null;

            if (args.Length == 2)
            {
                if (!IdAccessCommandUtilities.TryParseAccessLevel(args[1], out AccessLevel level, out string error))
                {
                    return response.MakeInvalid(error);
                }

                testLevel = level;
            }
            else if (args.Length == 1)
            {
                if (IdAccessCommandUtilities.TryParseAccessLevel(args[0], out AccessLevel level, out _))
                {
                    ckey = null;
                    testLevel = level;
                }
            }

            return response.MakeValid(new CalculatedValues(ckey, testLevel));
        }
    }
}
