using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.ScreenEffects;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Debug command to trigger the momentary hit-flash screen effect on the caller's own screen.
    /// </summary>
    public class ScreenEffectHitFlashCommand : Command
    {
        public override string ShortDescription => "Trigger the hit-flash screen effect";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues _)) return response.InvalidArgs;

            SubSystems.Get<ScreenEffectsSubSystem>()?.TriggerHitFlash();

            return "Hit flash triggered";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 0) return response.MakeInvalid("Invalid number of arguments");

            return response.MakeValid(new CalculatedValues());
        }
    }
}
