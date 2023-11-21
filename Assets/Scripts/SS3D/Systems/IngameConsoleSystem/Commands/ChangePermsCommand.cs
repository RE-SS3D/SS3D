﻿using System;
using System.Linq;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class ChangePermsCommand : Command
    {
        public override string LongDescription => "changeperms (user ckey) (required role)";
        public override string ShortDescription => "Change user permission";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        
        private struct CalculatedValues : ICalculatedValues
        {
            public string Ckey;
            public ServerRoleTypes Role;
        }

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues calculatedValues)) return response.InvalidArgs;

            Subsystems.Get<PermissionSystem>().ChangeUserPermission(calculatedValues.Ckey, calculatedValues.Role);
            return "Permission changed to " + args[1];
        }
        
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            if (args.Length != 2) return response.MakeInvalid("Invalid number of arguments");

            ServerRoleTypes role = FindRole(args[1]);
            if (role == ServerRoleTypes.None) return response.MakeInvalid("Role doesn't exist");
            
            return response.MakeValid(new CalculatedValues{Ckey = args[0], Role = role});
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