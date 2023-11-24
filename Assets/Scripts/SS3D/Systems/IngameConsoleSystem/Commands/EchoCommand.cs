using System;
using System.Linq;
using FishNet.Connection;
using SS3D.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class EchoCommand : Command
    {
        public override string LongDescription => "echo (number) (your string)";
        public override string ShortDescription => "Repeat your string";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Server;
        
        private record CalculatedValues(UInt16 Number) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;
            
            return String.Concat(Enumerable.Repeat(string.Join(" ", args.Skip(1)), values.Number));
        }
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length != 2) return response.MakeInvalid("Invalid number of arguments");
            
            UInt16.TryParse(args[0], out UInt16 number);
            if (number == 0) return response.MakeInvalid("Invalid number");
            
            return response.MakeValid(new CalculatedValues(number));
        }
    }
}