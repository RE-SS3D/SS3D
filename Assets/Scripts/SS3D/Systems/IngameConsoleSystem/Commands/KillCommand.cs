using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class KillCommand : Command
    {
        public override string LongDescription => "kill (user ckey)";
        public override string ShortDescription => "Kill player";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;


        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            CheckArgsResponse checkArgsResponse = CheckArgs(args);
            if (checkArgsResponse.IsValid == false)
                return checkArgsResponse.InvalidArgs;
            string ckey = args[0];
            Player playerToKill= Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
            Entity entityToKill = Subsystems.Get<EntitySystem>().GetSpawnedEntity(playerToKill);
            entityToKill.Kill();
            return "Player killed";
        }

        [Server]
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
            Player PlayerToKill = Subsystems.Get<PlayerSystem>().GetPlayer(ckey);
            if (PlayerToKill == null)
            {
                response.IsValid = false;
                response.InvalidArgs = "This player doesn't exist";
                return response;
            }
            Entity entityToKill = Subsystems.Get<EntitySystem>().GetSpawnedEntity(PlayerToKill);
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