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
    public class HealCommand : Command
    {
        public override string LongDescription => "Apply zone treatment to a player (stops bleeding by default)";
        public override string Usage => "(ckey) (BodyZone|all) (bruteHeal) (burnHeal)\nexample: heal editorUser Chest 50 0\nexample: heal editorUser all 999 999";
        public override string ShortDescription => "Apply zone treatment to a player";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(
            HumanHealthController Health,
            BodyZone? Zone,
            bool AllZones,
            float BruteHeal,
            float BurnHeal) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            if (values.AllZones)
            {
                for (int i = 0; i < HealthConstants.ZoneCount; i++)
                {
                    values.Health.ApplyTreatment((BodyZone)i, values.BruteHeal, values.BurnHeal, stopBleeding: true);
                }

                values.Health.RestoreSystemicPools();
                values.Health.RestoreOrgans();
                return "Player fully healed";
            }

            values.Health.ApplyTreatment(values.Zone.Value, values.BruteHeal, values.BurnHeal, stopBleeding: true);
            return "Player healed";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 4)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            bool allZones = args[1].Equals("all", StringComparison.OrdinalIgnoreCase);
            BodyZone? zone = null;

            if (!allZones)
            {
                if (!Enum.TryParse(args[1], true, out BodyZone parsedZone))
                {
                    return response.MakeInvalid("Invalid body zone (use Head, Chest, LeftArm, RightArm, LeftLeg, RightLeg, Groin, or all)");
                }

                zone = parsedZone;
            }

            if (!float.TryParse(args[2], out float bruteHeal))
            {
                return response.MakeInvalid("Invalid brute heal amount");
            }

            if (!float.TryParse(args[3], out float burnHeal))
            {
                return response.MakeInvalid("Invalid burn heal amount");
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

            return response.MakeValid(new CalculatedValues(health, zone, allZones, bruteHeal, burnHeal));
        }
    }
}
