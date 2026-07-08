using SS3D.UI.MachineInterface.Components;
using UnityEngine.UIElements;

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

        public static StatusTone GetIoTone(string word)
        {
            string upper = word.ToUpperInvariant();
            if (upper.Contains("NO SIGNAL"))
            {
                return StatusTone.Warning;
            }

            if (upper.Contains("DISABLED") || upper.Contains("CUTOFF"))
            {
                return StatusTone.Danger;
            }

            if (upper.Contains("OVERDRAWN"))
            {
                return StatusTone.Warning;
            }

            return StatusTone.Success;
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

        public static void ApplyFillTone(VisualElement fill, StatusTone tone)
        {
            fill.RemoveFromClassList("tone-success");
            fill.RemoveFromClassList("tone-warning");
            fill.RemoveFromClassList("tone-danger");
            fill.AddToClassList(tone switch
            {
                StatusTone.Danger => "tone-danger",
                StatusTone.Warning => "tone-warning",
                _ => "tone-success",
            });
        }

        public static void ApplyIoCardTone(VisualElement card, StatusTone tone)
        {
            card.RemoveFromClassList("tone-success");
            card.RemoveFromClassList("tone-warning");
            card.RemoveFromClassList("tone-danger");
            card.RemoveFromClassList("tone-info");
            card.AddToClassList(tone switch
            {
                StatusTone.Danger => "tone-danger",
                StatusTone.Warning => "tone-warning",
                StatusTone.Info => "tone-info",
                _ => "tone-success",
            });
        }
    }
}
