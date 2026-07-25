using System;
using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class HoldPoseCommand : Command
    {
        public override string ShortDescription => "Set an arm's hold pose";
        public override string Usage => "(left|right) (none|briefcase|drink|underarm|shoulder|waiter)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues(HumanoidAnimatorController Controller, HandSide Side, HoldPose Pose) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            values.Controller.SetHoldPose(values.Side, values.Pose);
            return $"{values.Side} hold pose set to {values.Pose}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 2) return response.MakeInvalid("Invalid number of arguments");

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

            if (!Enum.TryParse(args[1], true, out HoldPose pose))
                return response.MakeInvalid("Invalid pose. Use none, briefcase, drink, underarm, shoulder, or waiter");

            if (!LocalHumanoidAnimatorResolver.TryGetLocalHumanoidAnimatorController(out HumanoidAnimatorController controller))
                return response.MakeInvalid("No locally controlled entity found");

            return response.MakeValid(new CalculatedValues(controller, side, pose));
        }
    }
}
