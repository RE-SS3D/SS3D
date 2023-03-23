using System.Collections.Generic;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class SoulListCommand : Command
    {
        public override string LongDescription => "Show all souls";
        public override string ShortDescription => "Show all souls";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override string Perform(string[] args)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;
            
            string ret = "";
            IEnumerable<Soul> souls = Subsystems.Get<PlayerSystem>().ServerSouls;
            foreach (Soul i in souls)
            {
                ret += i.Ckey + "\t";
            }
            return ret;
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