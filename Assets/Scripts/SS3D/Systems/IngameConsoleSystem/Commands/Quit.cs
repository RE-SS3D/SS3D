using System;
using SS3D.Systems.Permissions;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class Quit : Command
    {
        public override string LongDescription => "Close app";
        public override string ShortDescription => "Close app";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override string Perform(string[] args)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;
            
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