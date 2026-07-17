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
    public class SeverCommand : Command
    {
        public override string LongDescription => "Sever a body zone on a player (requires Disabled tier unless force)";
        public override string Usage => "(ckey) (BodyZone) [force]\nexample: sever editorUser LeftArm force";
        public override string ShortDescription => "Sever a limb or head";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(HumanHealthController Health, BodyZone Zone, bool Force) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            if (!values.Health.TrySeverZone(values.Zone, values.Force))
            {
                return "Severance failed — zone not severable, already severed, or not Disabled yet";
            }

            return $"Severed {values.Zone}";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length < 2 || args.Length > 3)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            if (!Enum.TryParse(args[1], true, out BodyZone zone))
            {
                return response.MakeInvalid("Invalid body zone");
            }

            bool force = args.Length == 3 && args[2].Equals("force", StringComparison.OrdinalIgnoreCase);

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

            return response.MakeValid(new CalculatedValues(health, zone, force));
        }
    }
}
