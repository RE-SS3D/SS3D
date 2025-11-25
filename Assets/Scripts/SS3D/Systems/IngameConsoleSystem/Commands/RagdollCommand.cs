using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using System.Globalization;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class RagdollCommand : Command
    {
        private record CalculatedValues(Entity Entity, float Time) : ICalculatedValues;

        public override string ShortDescription => "Toggle player's ragdoll";

        public override string Usage => "(ckey) [time]. Time is a float type and should be written as 0.5";

        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;

        public override CommandType Type => CommandType.Server;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            if (values.Entity is not IRagdollable ragdollable)
            {
                return "entity is not ragdollable";
            }

            if (args.Length > 1)
            {
                ragdollable.Knockdown(10);
            }
            else if (ragdollable.IsRagdolled())
            {
                ragdollable.Recover();
            }
            else
            {
                ragdollable.Knockdown(10);
            }

            return "Player ragdolled";
        }

        [Server]
        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = default;

            if (args.Length < 1 || args.Length > 2)
            {
                return response.MakeInvalid("Invalid number of arguments");
            }

            string ckey = args[0];
            Player player = Subsystems.Get<PlayerSubSystem>().GetPlayer(ckey);

            if (player == null)
            {
                return response.MakeInvalid("This player doesn't exist");
            }

            Entity entity = Subsystems.Get<EntitySubSystem>().GetSpawnedEntity(player);

            if (entity == null)
            {
                return response.MakeInvalid("This entity doesn't exist");
            }

            float time = 0;

            if (args.Length > 1)
            {
                // Use dot as separator
                NumberFormatInfo nfi = new();
                nfi.NumberDecimalSeparator = ".";

                if (float.TryParse(args[1], NumberStyles.Any, nfi, out time))
                {
                    if (time <= 0)
                    {
                        return response.MakeInvalid("Invalid time");
                    }
                }
                else
                {
                    return response.MakeInvalid("Invalid time");
                }
            }

            return response.MakeValid(new CalculatedValues(entity, time));
        }
    }
}
