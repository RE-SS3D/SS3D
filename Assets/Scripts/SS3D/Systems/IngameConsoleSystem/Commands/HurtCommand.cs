using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.Health;
using SS3D.Systems.PlayerControl;
using System;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class HurtCommand : Command
    {
        public override string LongDescription => "Apply zone damage to a player";
        public override string Usage => "(ckey) (BodyZone) (brute) (burn)\nexample: hurt editorUser Chest 25 0";
        public override string ShortDescription => "Apply zone damage to a player";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(HumanHealthController Health, BodyZone Zone, float Brute, float Burn) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            values.Health.ApplyDamage(values.Zone, values.Brute, values.Burn);
            return "Player hurt";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 4)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            if (!Enum.TryParse(args[1], true, out BodyZone zone))
            {
                return response.MakeInvalid("Invalid body zone");
            }

            if (!float.TryParse(args[2], out float brute))
            {
                return response.MakeInvalid("Invalid brute amount");
            }

            if (!float.TryParse(args[3], out float burn))
            {
                return response.MakeInvalid("Invalid burn amount");
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

            return response.MakeValid(new CalculatedValues(health, zone, brute, burn));
        }
    }
}
