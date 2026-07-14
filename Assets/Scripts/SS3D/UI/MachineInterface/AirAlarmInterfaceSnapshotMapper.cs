using SS3D.Systems.Atmospherics.Pipes;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    public static class AirAlarmInterfaceSnapshotMapper
    {
        public static AirAlarmInterfaceViewModel ToViewModel(AirAlarmInterfaceSnapshot snapshot)
        {
            List<AirAlarmDeviceSnapshot> connectedDevices = snapshot.ConnectedDevices ?? new List<AirAlarmDeviceSnapshot>();

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
            model.AccessDenied = snapshot.AccessDenied;
            model.Scenario = scenario;

            if (snapshot.HasSample)
            {
                ApplyLiveReadings(model, snapshot);
            }

            model.ActiveMode = (AirAlarmPresetMode)snapshot.ActiveMode;
            model.SelectedDeviceId = string.IsNullOrEmpty(snapshot.SelectedDeviceId)
                ? null
                : snapshot.SelectedDeviceId;

            model.ConnectedDevices.Clear();
            foreach (AirAlarmDeviceSnapshot deviceSnapshot in connectedDevices)
            {
                model.ConnectedDevices.Add(AirAlarmInterfaceSnapshotSerializer.ToConnectedDevice(deviceSnapshot));
            }

            if (model.ConnectedDevices.Count > 0)
            {
                model.DeviceCountText = $"{model.ConnectedDevices.Count} devices linked";
            }

            return model;
        }

        private static void ApplyLiveReadings(AirAlarmInterfaceViewModel model, AirAlarmInterfaceSnapshot snapshot)
        {
            model.PressureText = $"{snapshot.PressureKpa:0.0} kPa";
            model.PressureTone = DerivePressureTone(snapshot.PressureKpa);

            float temperatureC = snapshot.TemperatureKelvin - 273.15f;
            model.TemperatureText = $"{temperatureC:0.0}°C";
            model.TemperatureTone = DeriveTemperatureTone(snapshot.TemperatureKelvin);

            model.GasReadouts = new List<AirAlarmGasReadout>
            {
                CreateGasReadout("O2", snapshot.OxygenFraction, AirAlarmConstants.LowOxygenMoleFraction, belowThresholdIsBad: true),
                CreateGasReadout("N2", snapshot.NitrogenFraction),
                CreateGasReadout("CO2", snapshot.CarbonDioxideFraction, AirAlarmConstants.HighCarbonDioxideMoleFraction, belowThresholdIsBad: false),
                CreateGasReadout("Plasma", snapshot.PlasmaFraction, AirAlarmConstants.HighPlasmaMoleFraction, belowThresholdIsBad: false),
            };
        }

        private static AirAlarmGasReadout CreateGasReadout(
            string label,
            float moleFraction,
            float threshold = 0f,
            bool belowThresholdIsBad = false)
        {
            float percent = ToDisplayPercent(moleFraction);
            float normalizedFraction = percent / 100f;
            StatusTone tone = StatusTone.Info;
            if (threshold > 0f)
            {
                if (belowThresholdIsBad && normalizedFraction < threshold)
                {
                    tone = StatusTone.Warning;
                }
                else if (!belowThresholdIsBad && normalizedFraction > threshold)
                {
                    tone = label == "Plasma" ? StatusTone.Danger : StatusTone.Warning;
                }
            }

            return new AirAlarmGasReadout
            {
                Label = label,
                Percent = percent,
                ValueTone = tone == StatusTone.Info ? StatusTone.Success : tone,
                BarTone = label == "O2" && tone == StatusTone.Info ? StatusTone.Success : tone,
            };
        }

        private static float ToDisplayPercent(float moleFractionOrPercent)
        {
            if (moleFractionOrPercent <= 1f)
            {
                return moleFractionOrPercent * 100f;
            }

            return Mathf.Clamp(moleFractionOrPercent, 0f, 100f);
        }

        private static StatusTone DerivePressureTone(float pressureKpa)
        {
            if (pressureKpa > AirAlarmConstants.HighPressureKpa
                || pressureKpa < AirAlarmConstants.LowPressureKpa)
            {
                return StatusTone.Warning;
            }

            return StatusTone.Success;
        }

        private static StatusTone DeriveTemperatureTone(float temperatureKelvin)
        {
            return temperatureKelvin > AirAlarmConstants.HighTemperatureKelvin
                ? StatusTone.Warning
                : StatusTone.Success;
        }
    }
}
