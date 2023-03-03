﻿using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class Kill : Command
    {
        public override string LongDescription => "kill (user ckey)";
        public override string ShortDescription => "Kill player";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override string Perform(string[] args)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;
            string ckey = args[0];
            Soul PlayerToKill = SystemLocator.Get<PlayerSystem>().GetSoul(ckey);
            Entity entityToKill = SystemLocator.Get<EntitySystem>().GetSpawnedEntity(PlayerToKill);
            entityToKill.GetComponent<HealthController>().ClientKill();
            return "Player killed";
        }
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new CheckArgsResponse();
            if (args.Length != 1)
            {
                response.IsValid = false;
                response.InvalidArgs = "Invalid number of arguments";
                return response;
            }
            string ckey = args[0];
            Soul PlayerToKill = SystemLocator.Get<PlayerSystem>().GetSoul(ckey);
            if (PlayerToKill == null)
            {
                response.IsValid = false;
                response.InvalidArgs = "This player doesn't exist";
                return response;
            }
            Entity entityToKill = SystemLocator.Get<EntitySystem>().GetSpawnedEntity(PlayerToKill);
            if (entityToKill == null)
            {
                response.IsValid = false;
                response.InvalidArgs = "This entity doesn't exist";
                return response;
            }
            response.IsValid = true;
            return response;
        }
    }
}