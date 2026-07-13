using SS3D.Systems.Atmospherics.Pipes;

namespace SS3D.UI.MachineInterface
{
    public static class AirAlarmInterfaceSnapshotMapper
    {
        public static AirAlarmInterfaceViewModel ToViewModel(AirAlarmInterfaceSnapshot snapshot)
        {
            AirAlarmScenario scenario = (AirAlarmScenario)snapshot.Scenario;
            AirAlarmInterfaceViewModel model = scenario switch
            {
                AirAlarmScenario.Warning => AirAlarmInterfaceViewModel.CreateWarning(),
                AirAlarmScenario.Danger => AirAlarmInterfaceViewModel.CreateDanger(),
                _ => AirAlarmInterfaceViewModel.CreateNormal(),
            };

            model.Title = snapshot.Title;
            model.ModelLabel = snapshot.ModelLabel;
            model.DeviceTitle = snapshot.DeviceTitle;
            model.Subtitle = snapshot.Subtitle;
            model.ChassisPowerOk = snapshot.PowerOk;
            model.AccessGranted = snapshot.AccessGranted;
            model.AccessScanning = snapshot.AccessScanning;

            if (snapshot.PressureKpa > 0f)
            {
                model.PressureText = $"{snapshot.PressureKpa:0.0} kPa";
            }

            if (snapshot.OxygenFraction > 0f)
            {
                float oxygenPercent = snapshot.OxygenFraction * 100f;
                foreach (AirAlarmGasReadout readout in model.GasReadouts)
                {
                    if (readout.Label == "O2")
                    {
                        readout.Percent = oxygenPercent;
                    }
                }
            }

            if (snapshot.CarbonDioxideFraction > 0f)
            {
                float co2Percent = snapshot.CarbonDioxideFraction * 100f;
                foreach (AirAlarmGasReadout readout in model.GasReadouts)
                {
                    if (readout.Label == "CO2")
                    {
                        readout.Percent = co2Percent;
                    }
                }
            }

            if (scenario == AirAlarmScenario.Warning && snapshot.PressureKpa > 0f)
            {
                model.PressureText = $"{snapshot.PressureKpa:0.0} kPa";
            }

            return model;
        }
    }
}
