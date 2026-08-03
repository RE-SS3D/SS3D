using System;
using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class FlinchCommand : Command
    {
        public override string ShortDescription => "Play a one-shot flinch reaction on yourself";
        public override string Usage => "(armleft|armright|legleft|legright|headfront|headback|torsofront|torsoback)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues(HumanoidAnimatorController Controller, FlinchRegion Region) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            values.Controller.TriggerFlinch(values.Region);
            return $"Triggered {values.Region} flinch";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 1) return response.MakeInvalid("Invalid number of arguments");

            if (!Enum.TryParse(args[0], true, out FlinchRegion region)) return response.MakeInvalid("Invalid region");

            if (!LocalHumanoidAnimatorResolver.TryGetLocalHumanoidAnimatorController(out HumanoidAnimatorController controller))
                return response.MakeInvalid("No locally controlled entity found");

            return response.MakeValid(new CalculatedValues(controller, region));
        }
    }
}
