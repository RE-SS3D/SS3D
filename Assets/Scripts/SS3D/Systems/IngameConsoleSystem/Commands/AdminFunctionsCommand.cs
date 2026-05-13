using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class AdminFunctionsCommand : Command
    {
        public override string ShortDescription => "View or toggle admin functions for all users";
        public override string Usage => "[on/off]";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.ServerOwner;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(bool ShouldSet, bool Enabled) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();

            if (!values.ShouldSet)
            {
                return $"Admin functions for all users: {FormatState(permissionSystem.AdminFunctionsEnabledForAll)}";
            }

            if (conn != null && !conn.IsHost)
            {
                return "Only the host can toggle admin functions for all users";
            }

            permissionSystem.SetAdminFunctionsEnabledForAll(values.Enabled);
            return $"Admin functions for all users: {FormatState(values.Enabled)}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length == 0)
            {
                return response.MakeValid(new CalculatedValues(false, false));
            }

            if (args.Length != 1)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            if (!TryParseState(args[0], out bool enabled))
            {
                return response.MakeInvalid("Use on/off, true/false, or 1/0");
            }

            return response.MakeValid(new CalculatedValues(true, enabled));
        }

        private static bool TryParseState(string value, out bool enabled)
        {
            switch (value.ToLowerInvariant())
            {
                case "on":
                case "true":
                case "1":
                case "enable":
                case "enabled":
                    enabled = true;
                    return true;
                case "off":
                case "false":
                case "0":
                case "disable":
                case "disabled":
                    enabled = false;
                    return true;
                default:
                    enabled = false;
                    return false;
            }
        }

        private static string FormatState(bool enabled)
        {
            return enabled ? "enabled" : "disabled";
        }
    }
}
