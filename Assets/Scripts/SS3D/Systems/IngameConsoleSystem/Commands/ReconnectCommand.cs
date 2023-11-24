using System.Diagnostics;
using FishNet.Connection;
using SS3D.Permissions;
using UnityEngine.Device;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class ReconnectCommand: Command
    {
        public override string LongDescription => "Restart app";
        public override string ShortDescription => "Restart app";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Offline;

        private record CalculatedValues : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;
            
            Process.Start(Application.dataPath.Replace("_Data", ".exe"));
            Application.Quit();
            return "Done";
        }
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new ();
            if (args.Length != 0) return response.MakeInvalid("Invalid number of arguments");

            return response.MakeValid(new CalculatedValues());
        }
    }
}