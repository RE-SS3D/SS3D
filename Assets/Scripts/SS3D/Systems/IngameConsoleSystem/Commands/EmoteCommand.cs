using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Entities.Humanoid;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class EmoteCommand : Command
    {
        public override string ShortDescription => "Play an emote by name";
        public override string Usage => "(emote name)";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues(HumanoidAnimatorController Controller, EmoteData Emote) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            return values.Controller.TryPlayEmote(values.Emote)
                ? $"Playing emote {values.Emote.EmoteName}"
                : $"Can't play {values.Emote.EmoteName} in your current posture";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 1) return response.MakeInvalid("Invalid number of arguments");

            if (!LocalHumanoidAnimatorResolver.TryGetLocalHumanoidAnimatorController(out HumanoidAnimatorController controller))
                return response.MakeInvalid("No locally controlled entity found");

            if (!controller.TryGetEmote(args[0], out EmoteData emote))
                return response.MakeInvalid("No such emote");

            return response.MakeValid(new CalculatedValues(controller, emote));
        }
    }
}
