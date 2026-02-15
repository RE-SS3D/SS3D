using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Health;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class KillCommand : Command
    {
        public override string ShortDescription => "Kill player";
        public override string Usage => "(ckey)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(Entity Entity) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;
            
            values.Entity.Kill();
            return "Player killed";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();
            
            if (args.Length != 1) return response.MakeInvalid("Invalid number of arguments");
            
            Player playerToKill = SubSystems.Get<PlayerSubSystem>().GetPlayer(args[0]);
            if (playerToKill == null) return response.MakeInvalid("This player doesn't exist");
            
            Entity entityToKill = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(playerToKill);
            if (entityToKill == null) return response.MakeInvalid("This entity doesn't exist");
            
            return response.MakeValid(new CalculatedValues(entityToKill));
        }
    }
}