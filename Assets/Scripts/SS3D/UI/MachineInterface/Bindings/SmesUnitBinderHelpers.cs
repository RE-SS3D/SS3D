using SS3D.UI.MachineInterface.Components;

namespace SS3D.UI.MachineInterface.Bindings
{
    internal static class SmesUnitBinderHelpers
    {
        public static StatusTone GetStatusTone(SmesPowerState state)
        {
            return state switch
            {
                SmesPowerState.Fault => StatusTone.Danger,
                SmesPowerState.Overload => StatusTone.Warning,
                SmesPowerState.Degraded => StatusTone.Warning,
                _ => StatusTone.Success,
            };
        }

        public static StatusTone GetChargeTone(float chargePct)
        {
            if (chargePct <= 0.15f)
            {
                return StatusTone.Danger;
            }

            if (chargePct <= 0.4f)
            {
                return StatusTone.Warning;
            }

            return StatusTone.Success;
        }

        public static StatusTone GetInputTone(SmesInterfaceViewModel model)
        {
            if (model.InputActive)
            {
                return StatusTone.Success;
            }

            if (model.InputEnabled)
            {
                return StatusTone.Warning;
            }

            return StatusTone.Info;
        }

        public static StatusTone GetOutputTone(SmesInterfaceViewModel model)
        {
            if (model.State == SmesPowerState.Overload)
            {
                return StatusTone.Warning;
            }

            if (model.OutputActive)
            {
                return StatusTone.Success;
            }

            return StatusTone.Info;
        }

        public static string GetTrendText(SmesChargeTrend trend)
        {
            return trend switch
            {
                SmesChargeTrend.Draining => "draining",
                SmesChargeTrend.DrainingFast => "draining fast",
                SmesChargeTrend.Critical => "critical",
                _ => "steady",
            };
        }
    }
}
