using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.IdAccess;

namespace SS3D.Systems.IngameConsoleSystem.Commands.IdAccessCommands
{
    public class AccessGrantCommand : Command
    {
        public override string ShortDescription => "Grant an access level to on-person ID record";
        public override string LongDescription =>
            "Adds one access level to the crew record bound to the player's currently resolved ID card. "
            + "Use accesscheck first to confirm which card and record are active.";
        public override string Usage => "(level) [ckey]";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(string Ckey, AccessLevel Level) : ICalculatedValues;

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

            if (!AccessCredentialResolver.TryResolveBoundRecord(
                    inventory,
                    out CrewRecordId recordId,
                    out _))
            {
                return "No bound ID card found on person.";
            }

            IdAccessSubSystem idAccess = SubSystems.Get<IdAccessSubSystem>();
            if (!idAccess.TryGetRecord(recordId, out CrewRecord record))
            {
                return $"Card points to missing crew record {recordId}.";
            }

            AccessMask updated = record.Access.With(values.Level);
            if (!IdAccessCommandUtilities.TrySetRecordAccess(inventory, updated, out string setError))
            {
                return setError;
            }

            return $"Granted {values.Level}. Record access is now: {IdAccessCommandUtilities.FormatAccessMask(updated)}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length < 1 || args.Length > 2)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            string levelArg = args[0];
            string ckey = args.Length == 2 ? args[1] : null;

            if (!IdAccessCommandUtilities.TryParseAccessLevel(levelArg, out AccessLevel level, out string error))
            {
                return response.MakeInvalid(error);
            }

            return response.MakeValid(new CalculatedValues(ckey, level));
        }
    }
}
