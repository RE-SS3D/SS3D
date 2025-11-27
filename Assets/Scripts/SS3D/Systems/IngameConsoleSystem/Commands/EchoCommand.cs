using FishNet.Connection;
using SS3D.Permissions;
using System.Linq;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class EchoCommand : Command
    {
        private record CalculatedValues(ushort Number) : ICalculatedValues;

        public override string ShortDescription => "Repeat your string";

        public override string Usage => "(number) (your string)";

        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;

        public override CommandType Type => CommandType.Server;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            return string.Concat(Enumerable.Repeat(string.Join(" ", args.Skip(1)), values.Number));
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = default;

            if (args.Length != 2)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            ushort.TryParse(args[0], out ushort number);

            if (number == 0)
            {
                return response.MakeInvalid("Invalid number");
            }

            return response.MakeValid(new CalculatedValues(number));
        }
    }
}
