using FishNet.Connection;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.IngameConsoleSystem.Commands;
using SS3D.UI.MainHud.Components;
using System;

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
        public override string ShortDescription => "Force an Alert Icon Stack hazard's severity";
        public override string Usage => "(hazard) (none|warning|critical)\n"
            + "hazards: fire, hot, cold, lowpressure, highpressure, radiation, hunger, thirst, pulling, "
            + "restrained, lowoxygen, dying, bleeding, cardiacarrest, sealbreach\n"
            + "example: alertstack fire critical";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues(AlertHazard Hazard, AlertSeverity Severity) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values)) return response.InvalidArgs;

            MainHudSubSystem mainHud = SubSystems.Get<MainHudSubSystem>();
            if (mainHud == null)
            {
                return "Main HUD subsystem not available";
            }

            AlertStackState state = mainHud.DebugAlertOverride;
            SetSeverity(ref state, values.Hazard, values.Severity);
            mainHud.SetDebugAlertOverride(state);

            return $"{values.Hazard} set to {values.Severity}";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 2) return response.MakeInvalid(WrongArgsText);

            if (!Enum.TryParse(args[0], true, out AlertHazard hazard)) return response.MakeInvalid("Invalid hazard name");

            if (!Enum.TryParse(args[1], true, out AlertSeverity severity)) return response.MakeInvalid("Invalid severity");

            return response.MakeValid(new CalculatedValues(hazard, severity));
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
            }
        }
    }
}
