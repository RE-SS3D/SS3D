using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class PostureCommand : Command
    {
        public override string ShortDescription => "Set your own posture (stand/sit/prone)";
        public override string Usage => "(stand|sit|prone)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues(HumanoidAnimatorController Controller, Posture Posture) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            values.Controller.SetPosture(values.Posture);
            return $"Posture set to {values.Posture}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 1) return response.MakeInvalid("Invalid number of arguments");

            if (!TryParsePosture(args[0], out Posture posture)) return response.MakeInvalid("Invalid posture. Use stand, sit, or prone");

            if (!LocalHumanoidAnimatorResolver.TryGetLocalHumanoidAnimatorController(out HumanoidAnimatorController controller))
                return response.MakeInvalid("No locally controlled entity found");

            return response.MakeValid(new CalculatedValues(controller, posture));
        }

        private static bool TryParsePosture(string arg, out Posture posture)
        {
            switch (arg.ToLowerInvariant())
            {
                case "stand":
                case "standing":
                case "upright":
                    posture = Posture.Standing;
                    return true;
                case "sit":
                case "sitting":
                    posture = Posture.Sitting;
                    return true;
                case "prone":
                    posture = Posture.Prone;
                    return true;
                default:
                    posture = default;
                    return false;
            }
        }
    }
}
