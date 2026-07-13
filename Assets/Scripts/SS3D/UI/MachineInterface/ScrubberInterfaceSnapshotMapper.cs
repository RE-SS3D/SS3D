using SS3D.Systems.Atmospherics.Pipes;

namespace SS3D.UI.MachineInterface
{
    public static class ScrubberInterfaceSnapshotMapper
    {
        public static ScrubberInterfaceViewModel ToViewModel(ScrubberInterfaceSnapshot snapshot)
        {
            ScrubberScenario scenario = (ScrubberScenario)snapshot.Scenario;
            ScrubberInterfaceViewModel model = scenario switch
            {
                ScrubberScenario.Idle => ScrubberInterfaceViewModel.CreateIdle(),
                ScrubberScenario.Overloaded => ScrubberInterfaceViewModel.CreateOverloaded(),
                ScrubberScenario.Fault => ScrubberInterfaceViewModel.CreateFault(),
                _ => ScrubberInterfaceViewModel.CreateFiltering(),
            };

            model.Title = snapshot.Title;
            model.ModelLabel = snapshot.ModelLabel;
            model.DeviceTitle = snapshot.DeviceTitle;
            model.Subtitle = snapshot.Subtitle;
            model.ChassisPowerOk = snapshot.PowerOk;
            model.Powered = snapshot.Powered;
            model.AccessGranted = snapshot.AccessGranted;
            model.AccessScanning = snapshot.AccessScanning;
            model.FlowRate = snapshot.FlowRate;
            ScrubberGasFilters.CopyToDictionary(
                snapshot.FilterO2,
                snapshot.FilterN2,
                snapshot.FilterCo2,
                snapshot.FilterPlasma,
                snapshot.FilterToxins,
                model.Filters);

            if (!snapshot.Connected && scenario is ScrubberScenario.Filtering or ScrubberScenario.Overloaded)
            {
                model.ConnectionStatus = "NO PIPE CONNECTION";
            }

            return model;
        }
    }
}
