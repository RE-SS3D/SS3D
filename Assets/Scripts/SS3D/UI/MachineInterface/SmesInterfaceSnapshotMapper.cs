using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public static class SmesInterfaceSnapshotMapper
    {
        public static SmesInterfaceViewModel ToViewModel(SmesInterfaceSnapshot snapshot)
        {
            SmesInterfaceViewModel model = new()
            {
                Title = snapshot.Title,
                State = (SmesPowerState)snapshot.PowerState,
                ChargePct = snapshot.ChargePct,
                ChargeTrend = (SmesChargeTrend)snapshot.ChargeTrend,
                InputCurrentKw = snapshot.InputCurrentKw,
                OutputCurrentKw = snapshot.OutputCurrentKw,
                InputMaxKw = snapshot.InputMaxKw,
                OutputMaxKw = snapshot.OutputMaxKw,
                InputEnabled = snapshot.InputEnabled,
                OutputEnabled = snapshot.OutputEnabled,
                InputActive = snapshot.InputActive,
                OutputActive = snapshot.OutputActive,
                ConnectionStateText = snapshot.ConnectionStateText,
                DiagnosisHint = snapshot.DiagnosisHint,
                MaintenanceText = snapshot.MaintenanceText,
                MaintenanceTone = (StatusTone)snapshot.MaintenanceTone,
                Warnings = BuildWarnings(snapshot),
                AdvancedMetrics = BuildAdvancedMetrics(snapshot),
            };

            ApplyStatusCopy(model);
            return model;
        }

        private static List<DiagnosticLine> BuildWarnings(SmesInterfaceSnapshot snapshot)
        {
            List<DiagnosticLine> lines = new(snapshot.WarningCount);
            for (int i = 0; i < snapshot.WarningCount; i++)
            {
                ApcDiagnosticSnapshot warning = GetWarning(snapshot, i);
                lines.Add(new DiagnosticLine(warning.Glyph, warning.Text, (StatusTone)warning.Tone));
            }

            return lines;
        }

        private static List<SmesMetricLine> BuildAdvancedMetrics(SmesInterfaceSnapshot snapshot)
        {
            List<SmesMetricLine> lines = new(snapshot.AdvancedMetricCount);
            for (int i = 0; i < snapshot.AdvancedMetricCount; i++)
            {
                SmesMetricSnapshot metric = GetMetric(snapshot, i);
                lines.Add(new SmesMetricLine(metric.Label, metric.Value, (StatusTone)metric.Tone));
            }

            return lines;
        }

        private static ApcDiagnosticSnapshot GetWarning(SmesInterfaceSnapshot snapshot, int index)
        {
            return index switch
            {
                0 => snapshot.Warning0,
                1 => snapshot.Warning1,
                2 => snapshot.Warning2,
                3 => snapshot.Warning3,
                _ => default,
            };
        }

        private static SmesMetricSnapshot GetMetric(SmesInterfaceSnapshot snapshot, int index)
        {
            return index switch
            {
                0 => snapshot.AdvancedMetric0,
                1 => snapshot.AdvancedMetric1,
                2 => snapshot.AdvancedMetric2,
                3 => snapshot.AdvancedMetric3,
                4 => snapshot.AdvancedMetric4,
                5 => snapshot.AdvancedMetric5,
                _ => default,
            };
        }

        private static void ApplyStatusCopy(SmesInterfaceViewModel model)
        {
            switch (model.State)
            {
                case SmesPowerState.Fault:
                {
                    model.StatusBadgeText = "FAULT";
                    model.ExteriorStatusWord = "SMES Offline";
                    break;
                }

                case SmesPowerState.Overload:
                {
                    model.StatusBadgeText = "OVERLOAD";
                    model.ExteriorStatusWord = "Overload Risk";
                    model.ExteriorOutputWord = "OVERDRAWN";
                    break;
                }

                case SmesPowerState.Degraded:
                {
                    model.StatusBadgeText = "DEGRADED";
                    model.ExteriorStatusWord = "Running on Reserve";
                    model.ExteriorInputWord = model.InputActive ? "AVAILABLE" : "NO SIGNAL";
                    break;
                }

                default:
                {
                    model.StatusBadgeText = "ONLINE";
                    model.ExteriorStatusWord = "SMES Online";
                    model.ExteriorInputWord = GetExteriorInputWord(model.InputEnabled, model.InputActive);
                    model.ExteriorOutputWord = GetExteriorOutputWord(model.OutputEnabled, model.OutputActive);
                    break;
                }
            }
        }

        private static string GetExteriorInputWord(bool inputEnabled, bool inputActive)
        {
            if (!inputEnabled)
            {
                return "DISABLED";
            }

            return inputActive ? "AVAILABLE" : "NO SIGNAL";
        }

        private static string GetExteriorOutputWord(bool outputEnabled, bool outputActive)
        {
            if (!outputEnabled)
            {
                return "DISABLED";
            }

            return outputActive ? "ACTIVE" : "DISABLED";
        }
    }
}
