using System;
using System.Linq;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Systems.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class ChangePermsCommand : Command
    {
        public override string LongDescription => "changeperms (user ckey) (required role)";
        public override string ShortDescription => "Change user permission";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override bool ServerCommand => true;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;
            string ckey = args[0];
            string role = args[1];
            ServerRoleTypes foundRole = FindRole(role);
            Subsystems.Get<PermissionSystem>().ChangeUserPermission(ckey, foundRole);
            return "Done";
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

            string ckey = args[0];
            bool isFound = Subsystems.Get<PermissionSystem>().TryGetUserRole(ckey, out _);
            if (!isFound)
            {
                response.IsValid = false;
                response.InvalidArgs = "Ckey doesn't have any permissions";
                return response;
            }

            string role = args[1];
            if (FindRole(role) == ServerRoleTypes.None)
            {
                response.IsValid = false;
                response.InvalidArgs = "Role doesn't exist";
                return response;
            }
            response.IsValid = true;
            return response;
        }

        private ServerRoleTypes FindRole(string name)
        {
            string[] roleNames = typeof(ServerRoleTypes).GetFields().Select(item => item.Name).ToArray();
            string foundRoleName = Array.Find(roleNames, item => item.ToLower() == name);
            if (foundRoleName != null)
            {
                ServerRoleTypes.TryParse(foundRoleName, out ServerRoleTypes foundRole);
                return foundRole;
            }
            return ServerRoleTypes.None;
        }
    }
}