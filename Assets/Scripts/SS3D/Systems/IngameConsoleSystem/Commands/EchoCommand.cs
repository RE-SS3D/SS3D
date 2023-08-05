using System;
using System.Linq;
using FishNet.Connection;
using SS3D.Systems.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class EchoCommand : Command
    {
        public override string LongDescription => "echo (number) (your string)";
        public override string ShortDescription => "Repeat your string";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
		public override bool ServerCommand => true;
		public override string Perform(string[] args, NetworkConnection conn = null)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;
            
            UInt16 number = UInt16.Parse(args[0]);
            return String.Concat(Enumerable.Repeat(string.Join(" ", args.Skip(1)), number));
        }
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new CheckArgsResponse();
            if (args.Length != 2)
            {
                response.IsValid = false;
                response.InvalidArgs = "Invalid number of arguments";
                return response;
            }
            UInt16.TryParse(args[0], out UInt16 number);
            if (number == 0)
            {
                response.IsValid = false;
                response.InvalidArgs = "Invalid number";
                return response;
            }

            response.IsValid = true;
            return response;
        }
    }
}