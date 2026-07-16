using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.IdAccess;

namespace SS3D.Systems.IngameConsoleSystem.Commands.IdAccessCommands
{
    public class AccessPresetCommand : Command
    {
        public override string ShortDescription => "Set on-person ID record access from a preset";
        public override string LongDescription =>
            "Replaces the crew record access mask with a named preset. "
            + "Presets: none, standard, engineer, security, hop, captain.";
        public override string Usage => "(preset) [ckey]";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(string Ckey, AccessMask Mask) : ICalculatedValues;

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

            if (!IdAccessCommandUtilities.TrySetRecordAccess(inventory, values.Mask, out string setError))
            {
                return setError;
            }

            return $"Access preset applied. Record access is now: {IdAccessCommandUtilities.FormatAccessMask(values.Mask)}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length < 1 || args.Length > 2)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            string presetArg = args[0];
            string ckey = args.Length == 2 ? args[1] : null;

            if (!IdAccessCommandUtilities.TryParsePreset(presetArg, out AccessMask mask, out string error))
            {
                return response.MakeInvalid(error);
            }

            return response.MakeValid(new CalculatedValues(ckey, mask));
        }
    }
}
