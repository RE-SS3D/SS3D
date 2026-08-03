using System.Globalization;
using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Entities.Humanoid;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class LimpCommand : Command
    {
        public override string ShortDescription => "Set your own leg limp amount";
        public override string Usage => "(amount), a float from -1 (left leg hurt) to 1 (right leg hurt), 0 = healthy";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues(HumanoidAnimatorController Controller, float Amount) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            values.Controller.SetLegInjury(values.Amount);
            return $"Leg injury set to {Mathf.Clamp(values.Amount, -1f, 1f)}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 1) return response.MakeInvalid("Invalid number of arguments");

            NumberFormatInfo numberFormat = new() { NumberDecimalSeparator = "." };
            if (!float.TryParse(args[0], NumberStyles.Any, numberFormat, out float amount))
                return response.MakeInvalid("Invalid amount, expected a number such as -1, 0, or 0.5");

            if (!LocalHumanoidAnimatorResolver.TryGetLocalHumanoidAnimatorController(out HumanoidAnimatorController controller))
                return response.MakeInvalid("No locally controlled entity found");

            return response.MakeValid(new CalculatedValues(controller, amount));
        }
    }
}
