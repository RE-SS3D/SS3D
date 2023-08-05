using System.Diagnostics;
using SS3D.Systems.Permissions;
using UnityEngine.Device;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class ReconnectCommand: Command
    {
        public override string LongDescription => "Restart app";
        public override string ShortDescription => "Restart app";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;

		public override bool ServerCommand => true;

		public override string Perform(string[] args)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;
            
            Process.Start(Application.dataPath.Replace("_Data", ".exe"));
            Application.Quit();
            return "Done";
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