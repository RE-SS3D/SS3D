using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.ScreenEffects;
using System;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Debug command to trigger/tune a screen-space effect on the caller's own screen, without needing
    /// the health/atmospherics integration that will eventually drive these for real.
    /// </summary>
    public class ScreenEffectCommand : Command
    {
        public override string ShortDescription => "Set a screen-space effect's intensity";
        public override string Usage => "(effect name) (intensity 0-1)\n"
            + "effects: hotroom, onfire, coldroom, freezing, lowoxygen, dyingcritical, bloodlosstunnelvision, concussion, unconscious\n"
            + "example: screeneffect onfire 0.8";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues(ScreenEffectType Type, float Intensity) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            SubSystems.Get<ScreenEffectsSubSystem>()?.SetEffect(values.Type, values.Intensity);

            return $"{values.Type} set to {values.Intensity:0.00}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 2) return response.MakeInvalid(WrongArgsText);

            if (!Enum.TryParse(args[0], true, out ScreenEffectType type)) return response.MakeInvalid("Invalid effect name");

            if (!float.TryParse(args[1], out float intensity)) return response.MakeInvalid("Invalid intensity");

            return response.MakeValid(new CalculatedValues(type, intensity));
        }
    }
}
