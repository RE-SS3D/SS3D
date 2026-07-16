using SS3D.Systems.Atmospherics.Pipes;

namespace SS3D.UI.MachineInterface
{
    public static class PumpInterfaceSnapshotMapper
    {
        private const float MolesPerSecondToLitersPerSecond = 22.414f;

        public static PumpInterfaceViewModel ToViewModel(PumpInterfaceSnapshot snapshot)
        {
            PumpScenario scenario = (PumpScenario)snapshot.Scenario;
            PumpInterfaceViewModel model = scenario switch
            {
                PumpScenario.Pumping => PumpInterfaceViewModel.CreatePumping(),
                PumpScenario.Starved => PumpInterfaceViewModel.CreateStarved(),
                PumpScenario.Fault => PumpInterfaceViewModel.CreateFault(),
                _ => PumpInterfaceViewModel.CreateIdle(),
            };

            model.Title = snapshot.Title;
            model.ModelLabel = snapshot.ModelLabel;
            model.DeviceTitle = snapshot.DeviceTitle;
            model.Subtitle = snapshot.Subtitle;
            model.ChassisPowerOk = snapshot.PowerOk;
            model.Powered = snapshot.Powered;
            model.AccessGranted = snapshot.AccessGranted;
            model.AccessScanning = snapshot.AccessScanning;
            model.AccessDenied = snapshot.AccessDenied;
            model.TargetOutletPressureKpa = snapshot.TargetOutletPressureKpa;

            model.InletPressureText = $"{snapshot.InletPressureKpa:F1} kPa";
            model.OutletPressureText = $"{snapshot.OutletPressureKpa:F1} kPa";

            if (snapshot.InletPressureKpa < 20f)
            {
                model.InletTone = StatusTone.Warning;
            }

            if (snapshot.OutletPressureKpa > snapshot.TargetOutletPressureKpa && snapshot.TargetOutletPressureKpa > 0)
            {
                model.OutletTone = StatusTone.Danger;
            }

            if (scenario is PumpScenario.Pumping or PumpScenario.Starved)
            {
                float litersPerSecond = snapshot.FlowMolesPerSecond * MolesPerSecondToLitersPerSecond;
                string flowQualifier = scenario == PumpScenario.Starved ? "Restricted" : "Flowing";
                model.FlowStatusText = $"{flowQualifier} — {litersPerSecond:F1} L/s";
                model.FlowStatusTone = scenario == PumpScenario.Starved ? StatusTone.Warning : StatusTone.Success;
                model.FlowGlyph = "→";
                model.FlowTone = model.FlowStatusTone;
            }
            else if (scenario == PumpScenario.Fault)
            {
                float litersPerSecond = snapshot.FlowMolesPerSecond * MolesPerSecondToLitersPerSecond;
                if (litersPerSecond > 0f)
                {
                    model.FlowStatusText = $"Uncontrolled — {litersPerSecond:F1} L/s";
                }
            }
            else if (!snapshot.Powered)
            {
                model.StatusSubline = "Powered off — no throughput.";
                model.FlowStatusText = "No flow";
            }
            else if (!snapshot.Connected)
            {
                model.ConnectionStatus = "NO PIPE CONNECTION";
                model.StatusSubline = "Awaiting pipe network link.";
                model.FlowStatusText = "No flow";
            }
            else if (!AtmosPumpController.ShouldPumpToOutlet(snapshot.OutletPressureKpa, snapshot.TargetOutletPressureKpa))
            {
                model.StatusSubline = "Outlet at or above target — pump holding.";
                model.FlowStatusText = "No flow";
            }

            return model;
        }
    }
}
