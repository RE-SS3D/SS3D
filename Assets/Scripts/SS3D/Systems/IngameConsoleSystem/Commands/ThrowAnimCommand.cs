using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    // Debug-only: there is no real throw-item interaction/mechanic in the codebase yet (only Hit
    // exists), so this exists purely to exercise the throw animation until that gameplay lands.
    public class ThrowAnimCommand : Command
    {
        public override string ShortDescription => "Play a one-shot throw animation (debug)";
        public override string Usage => "(left|right)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues(HumanoidAnimatorController Controller, HandSide Side) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            values.Controller.TriggerThrowLocal(values.Side);
            return $"Triggered {values.Side} throw animation";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 1) return response.MakeInvalid("Invalid number of arguments");

            HandSide side;
            switch (args[0].ToLowerInvariant())
            {
                case "left":
                    side = HandSide.Left;
                    break;
                case "right":
                    side = HandSide.Right;
                    break;
                default:
                    return response.MakeInvalid("Invalid side. Use left or right");
            }

            if (!LocalHumanoidAnimatorResolver.TryGetLocalHumanoidAnimatorController(out HumanoidAnimatorController controller))
                return response.MakeInvalid("No locally controlled entity found");

            return response.MakeValid(new CalculatedValues(controller, side));
        }
    }
}
