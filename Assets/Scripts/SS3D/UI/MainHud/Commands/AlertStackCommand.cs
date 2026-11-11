using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.IngameConsoleSystem.Commands;
using SS3D.UI.MainHud.Components;
using System.Collections.Generic;

namespace SS3D.UI.MainHud.Commands
{
    /// <summary>
    /// Debug command to force an Alert Icon Stack hazard's severity on the caller's own HUD, without needing
    /// the hunger/thirst/pressure/radiation/pulling/restrained/low-oxygen/dying trackers that don't exist yet.
    /// Lives in <c>SS3D.UI.MainHud</c> (not <c>SS3D.Systems</c>) because MainHud already references Systems;
    /// the reverse would recreate the asmdef cycle that forced Main HUD into its own assembly.
    /// </summary>
    public class AlertStackCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Declared = new[]
        {
            P.Enum<AlertHazard>("hazard"),
            P.Enum<AlertSeverity>("severity"),
        };

        public override string Name => "alertstack";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Force an Alert Icon Stack hazard's severity on your own HUD";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;
        public override IReadOnlyList<CommandParameter> Parameters => Declared;
        public override string Example => "fire critical";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            MainHudSubSystem mainHud = SubSystems.Get<MainHudSubSystem>();
            if (mainHud == null)
            {
                return CommandResult.Error("Main HUD subsystem not available");
            }

            AlertHazard hazard = args.Get<AlertHazard>("hazard");
            AlertSeverity severity = args.Get<AlertSeverity>("severity");
            AlertStackState state = mainHud.DebugAlertOverride;
            SetSeverity(ref state, hazard, severity);
            mainHud.SetDebugAlertOverride(state);

            return CommandResult.Success($"{hazard} set to {severity}");
        }

        private static void SetSeverity(ref AlertStackState state, AlertHazard hazard, AlertSeverity severity)
        {
            switch (hazard)
            {
                case AlertHazard.Fire: state.Fire = severity; break;
                case AlertHazard.Hot: state.Hot = severity; break;
                case AlertHazard.Cold: state.Cold = severity; break;
                case AlertHazard.LowPressure: state.LowPressure = severity; break;
                case AlertHazard.HighPressure: state.HighPressure = severity; break;
                case AlertHazard.Radiation: state.Radiation = severity; break;
                case AlertHazard.Hunger: state.Hunger = severity; break;
                case AlertHazard.Thirst: state.Thirst = severity; break;
                case AlertHazard.Pulling: state.Pulling = severity; break;
                case AlertHazard.Restrained: state.Restrained = severity; break;
                case AlertHazard.LowOxygen: state.LowOxygen = severity; break;
                case AlertHazard.Dying: state.Dying = severity; break;
                case AlertHazard.Bleeding: state.Bleeding = severity; break;
                case AlertHazard.CardiacArrest: state.CardiacArrest = severity; break;
                case AlertHazard.SealBreach: state.SealBreach = severity; break;
                case AlertHazard.Invincible: state.Invincible = severity; break;
                case AlertHazard.TankLow: state.TankLow = severity; break;
            }
        }
    }
}
