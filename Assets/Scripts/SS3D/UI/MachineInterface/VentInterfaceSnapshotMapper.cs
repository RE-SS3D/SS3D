namespace SS3D.UI.MachineInterface
{
    public static class VentInterfaceSnapshotMapper
    {
        public static VentInterfaceViewModel ToViewModel(VentInterfaceSnapshot snapshot)
        {
            VentScenario scenario = (VentScenario)snapshot.Scenario;
            VentInterfaceViewModel model = scenario switch
            {
                VentScenario.Pressurizing => VentInterfaceViewModel.CreatePressurizing(),
                VentScenario.Depressurizing => VentInterfaceViewModel.CreateDepressurizing(),
                VentScenario.Fault => VentInterfaceViewModel.CreateFault(),
                _ => VentInterfaceViewModel.CreateIdle(),
            };

            model.Title = snapshot.Title;
            model.ModelLabel = snapshot.ModelLabel;
            model.DeviceTitle = snapshot.DeviceTitle;
            model.Subtitle = snapshot.Subtitle;
            model.ChassisPowerOk = snapshot.PowerOk;
            model.Powered = snapshot.Powered;
            model.AccessGranted = snapshot.AccessGranted;
            model.AccessScanning = snapshot.AccessScanning;
            model.TargetPressureKpa = snapshot.TargetPressureKpa;

            if (!snapshot.Connected && scenario is VentScenario.Pressurizing or VentScenario.Depressurizing)
            {
                model.ConnectionStatus = "NO PIPE CONNECTION";
            }

            return model;
        }
    }
}
