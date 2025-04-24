using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class PlayerListCommand : Command
    {
        public override string ShortDescription => "Show all players online";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            string ret = "";
            List<Player> players = SubSystems.Get<PlayerSubSystem>().OnlinePlayers.ToList();
            foreach (Player i in players)
            {
                ret += i.Ckey + "\t";
            }
            return ret;
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new ();
            if (args.Length != 0) return response.MakeInvalid("Invalid number of arguments");

            return response.MakeValid(new CalculatedValues());
        }
    }
}