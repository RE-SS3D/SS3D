using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class PlayerListCommand : Command
    {
        public override string LongDescription => "Show all players online";
        public override string ShortDescription => "Show all players online";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;

		public override bool ServerCommand => false;

		public override string Perform(string[] args, NetworkConnection conn = null)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;

            string ret = "";
            List<Player> players = Subsystems.Get<PlayerSystem>().OnlinePlayers.ToList();
            foreach (Player i in players)
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