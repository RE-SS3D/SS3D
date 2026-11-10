using System.Collections.Generic;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.ScreenEffects;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Debug command to trigger/tune a screen-space effect on the caller's own screen, without needing
    /// the health/atmospherics integration that will eventually drive these for real.
    /// </summary>
    public class ScreenEffectCommand : Command
    {
        private IReadOnlyList<Subcommand> _subcommands;

        public override string Name => "screeneffect";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Override a screen-space effect's intensity on your own screen";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;
        public override string Example => "set onfire 0.8";

        public override IReadOnlyList<Subcommand> Subcommands => _subcommands ??= new[]
        {
            new Subcommand("set", "Turn the debug override on and set one effect", PerformSet,
                P.Enum<ScreenEffectType>("effect"), P.Float("intensity").Range(0, 1)),
            new Subcommand("release", "Return control to the health and atmos mappers", PerformRelease),
        };

        private static CommandResult PerformSet(CommandContext context, CommandArgs args)
        {
            ScreenEffectsSubSystem effects = SubSystems.Get<ScreenEffectsSubSystem>();
            if (effects == null)
            {
                return CommandResult.Error("ScreenEffectsSubSystem unavailable");
            }

            ScreenEffectType type = args.Get<ScreenEffectType>("effect");
            float intensity = args.Float("intensity");
            effects.SetDebugOverrideActive(true);
            effects.SetEffect(type, intensity);

            return CommandResult.Success($"{type} set to {intensity:0.00} (debug override on)");
        }

        private static CommandResult PerformRelease(CommandContext context, CommandArgs args)
        {
            ScreenEffectsSubSystem effects = SubSystems.Get<ScreenEffectsSubSystem>();
            if (effects == null)
            {
                return CommandResult.Error("ScreenEffectsSubSystem unavailable");
            }

            effects.SetDebugOverrideActive(false);
            return CommandResult.Success("Screen-effect debug override released");
        }
    }
}
