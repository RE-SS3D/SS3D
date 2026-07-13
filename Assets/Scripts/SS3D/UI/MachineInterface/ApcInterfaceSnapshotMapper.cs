using System.Collections.Generic;
using System.Electricity;

namespace SS3D.UI.MachineInterface
{
    public static class ApcInterfaceSnapshotMapper
    {
        public static ApcInterfaceViewModel ToViewModel(ApcInterfaceSnapshot snapshot)
        {
            ApcInterfaceViewModel model = new()
            {
                Title = snapshot.Title,
                State = (ApcPowerState)snapshot.PowerState,
                GridInputKw = snapshot.GridInputKw,
                LoadOutputKw = snapshot.LoadOutputKw,
                BatteryCharge = snapshot.BatteryCharge,
                BatteryStateText = GetBatteryStateText((ApcBatteryState)snapshot.BatteryState),
                BatteryEtaText = "—",
                LightingOn = (snapshot.Channels & ApcControlFlags.Lighting) != 0,
                EquipmentOn = (snapshot.Channels & ApcControlFlags.Equipment) != 0,
                EnvironmentOn = (snapshot.Channels & ApcControlFlags.Environment) != 0,
                LightingLoadKw = snapshot.LightingLoadKw,
                EquipmentLoadKw = snapshot.EquipmentLoadKw,
                EnvironmentLoadKw = snapshot.EnvironmentLoadKw,
                Diagnostics = BuildDiagnostics(snapshot),
            };

            ApplyStatusCopy(model);
            return model;
        }

        private static List<DiagnosticLine> BuildDiagnostics(ApcInterfaceSnapshot snapshot)
        {
            List<DiagnosticLine> lines = new(snapshot.DiagnosticCount);
            for (int i = 0; i < snapshot.DiagnosticCount; i++)
            {
                ApcDiagnosticSnapshot diagnostic = GetDiagnostic(snapshot, i);
                lines.Add(new DiagnosticLine(
                    diagnostic.Glyph,
                    diagnostic.Text,
                    (StatusTone)diagnostic.Tone));
            }

            return lines;
        }

        private static ApcDiagnosticSnapshot GetDiagnostic(ApcInterfaceSnapshot snapshot, int index)
        {
            return index switch
            {
                0 => snapshot.Diagnostic0,
                1 => snapshot.Diagnostic1,
                2 => snapshot.Diagnostic2,
                3 => snapshot.Diagnostic3,
                4 => snapshot.Diagnostic4,
                5 => snapshot.Diagnostic5,
                _ => default,
            };
        }

        private static string GetBatteryStateText(ApcBatteryState state)
        {
            return state switch
            {
                ApcBatteryState.Discharging => "DISCHARGING",
                ApcBatteryState.Critical => "CRITICAL",
                _ => "CHARGED",
            };
        }

        private static void ApplyStatusCopy(ApcInterfaceViewModel model)
        {
            switch (model.State)
            {
                case ApcPowerState.Critical:
                {
                    model.StatusHeadline = "Power Failure";
                    model.StatusExplanation = "No grid input detected.";
                    model.HeaderReadout = "NO GRID INPUT";
                    model.ChassisPowerOk = false;
                    break;
                }

                case ApcPowerState.Overload:
                {
                    model.StatusHeadline = "Running on Battery";
                    model.StatusExplanation = "Grid supply below connected load.";
                    model.HeaderReadout = "LOAD EXCEEDS SUPPLY";
                    break;
                }

                default:
                {
                    model.StatusHeadline = "Room Powered";
                    model.StatusExplanation = "Grid supply meets connected load.";
                    model.HeaderReadout = "GRID FEED NOMINAL";
                    break;
                }
            }
        }
    }
}
