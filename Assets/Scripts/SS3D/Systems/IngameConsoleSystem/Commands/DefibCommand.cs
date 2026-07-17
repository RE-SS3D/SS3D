using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Systems.PlayerControl;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class DefibCommand : Command
    {
        public override string LongDescription => "Apply chest defibrillation to a player (admin testing)";
        public override string Usage => "(ckey)\nexample: defib editorUser";
        public override string ShortDescription => "Defibrillate a player's chest";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(HumanHealthController Health) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            DefibrillatorOutcome outcome = values.Health.TryDefibrillate(BodyZone.Chest);
            return outcome switch
            {
                DefibrillatorOutcome.Success => "Heart restarted",
                DefibrillatorOutcome.NoResponse => "No response (brain dead)",
                DefibrillatorOutcome.UnnecessaryShock => "Unnecessary shock (burn applied)",
                _ => "Defibrillation failed",
            };
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 1)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(args[0]);
            if (player == null)
            {
                return response.MakeInvalid("This player doesn't exist");
            }

            Entity entity = SubSystems.Get<EntitySubSystem>().GetSpawnedEntity(player);
            if (entity == null)
            {
                return response.MakeInvalid("This entity doesn't exist");
            }

            if (!entity.TryGetComponent(out HumanHealthController health))
            {
                return response.MakeInvalid("Entity has no HumanHealthController");
            }

            return response.MakeValid(new CalculatedValues(health));
        }
    }
}
