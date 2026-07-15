using System;
using SS3D.Systems.Atmospherics.Pipes;

namespace SS3D.UI.MachineInterface
{
    internal static class AccessGatedOptimisticHelpers
    {
        public static void ToggleAccessScan(IAccessGatedInterfaceViewModel model)
        {
            if (model.AccessGranted)
            {
                model.AccessGranted = false;
                model.AccessScanning = false;
                model.AccessDenied = false;
            }
            else if (!model.AccessScanning)
            {
                model.AccessScanning = true;
                model.AccessDenied = false;
            }
        }
    }

    internal sealed class ApcOptimisticControlHandler : IMachineOptimisticControlHandler
    {
        public Type ViewModelType => typeof(ApcInterfaceViewModel);

        public void ApplyBool(object model, byte controlId, bool isOn, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is not ApcInterfaceViewModel apc || !apc.AccessGranted)
            {
                return;
            }

            string channelId = controlId switch
            {
                MachineInterfaceControlIds.Apc.Lighting => "lighting",
                MachineInterfaceControlIds.Apc.Equipment => "equipment",
                MachineInterfaceControlIds.Apc.Environment => "environment",
                _ => null,
            };

            if (channelId == null)
            {
                return;
            }

            switch (controlId)
            {
                case MachineInterfaceControlIds.Apc.Lighting:
                    apc.LightingOn = isOn;
                    break;
                case MachineInterfaceControlIds.Apc.Equipment:
                    apc.EquipmentOn = isOn;
                    break;
                case MachineInterfaceControlIds.Apc.Environment:
                    apc.EnvironmentOn = isOn;
                    break;
            }

            callbacks.ChannelToggled?.Invoke(channelId, isOn);
        }

        public void ApplyNumeric(object model, byte controlId, float delta, MachineOptimisticControlCallbacks callbacks)
        {
        }

        public void ApplyAction(object model, byte controlId, int value, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is ApcInterfaceViewModel apc && controlId == MachineInterfaceControlIds.Apc.ReadId)
            {
                AccessGatedOptimisticHelpers.ToggleAccessScan(apc);
            }
        }
    }

    internal sealed class SmesOptimisticControlHandler : IMachineOptimisticControlHandler
    {
        public Type ViewModelType => typeof(SmesInterfaceViewModel);

        public void ApplyBool(object model, byte controlId, bool isOn, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is not SmesInterfaceViewModel smes || !smes.AccessGranted)
            {
                return;
            }

            switch (controlId)
            {
                case MachineInterfaceControlIds.Smes.Input:
                    smes.InputEnabled = isOn;
                    break;
                case MachineInterfaceControlIds.Smes.Output:
                    smes.OutputEnabled = isOn;
                    break;
            }
        }

        public void ApplyNumeric(object model, byte controlId, float delta, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is not SmesInterfaceViewModel smes || !smes.AccessGranted)
            {
                return;
            }

            switch (controlId)
            {
                case MachineInterfaceControlIds.Smes.Input:
                    smes.InputMaxKw = Math.Max(1f, smes.InputMaxKw + delta);
                    break;
                case MachineInterfaceControlIds.Smes.Output:
                    smes.OutputMaxKw = Math.Max(1f, smes.OutputMaxKw + delta);
                    break;
            }
        }

        public void ApplyAction(object model, byte controlId, int value, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is SmesInterfaceViewModel smes && controlId == MachineInterfaceControlIds.Smes.ReadId)
            {
                AccessGatedOptimisticHelpers.ToggleAccessScan(smes);
            }
        }
    }

    internal sealed class VendingOptimisticControlHandler : IMachineOptimisticControlHandler
    {
        public Type ViewModelType => typeof(VendingInterfaceViewModel);

        public void ApplyBool(object model, byte controlId, bool isOn, MachineOptimisticControlCallbacks callbacks)
        {
        }

        public void ApplyNumeric(object model, byte controlId, float delta, MachineOptimisticControlCallbacks callbacks)
        {
        }

        public void ApplyAction(object model, byte controlId, int value, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is not VendingInterfaceViewModel vending)
            {
                return;
            }

            switch (controlId)
            {
                case MachineInterfaceControlIds.Vending.SelectProduct:
                    if (value >= 0 && value < vending.Products.Count && vending.Products[value].CanSelect)
                    {
                        vending.VendingProductIndex = value;
                    }

                    break;
                case MachineInterfaceControlIds.Vending.TakeTrayItem:
                    if (value >= 0 && value < vending.TrayItems.Count)
                    {
                        vending.TrayItems.RemoveAt(value);
                    }

                    break;
            }
        }
    }

    internal sealed class ScrubberOptimisticControlHandler : IMachineOptimisticControlHandler
    {
        public Type ViewModelType => typeof(ScrubberInterfaceViewModel);

        public void ApplyBool(object model, byte controlId, bool isOn, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is ScrubberInterfaceViewModel scrubber
                && controlId == MachineInterfaceControlIds.Atmos.Power
                && scrubber.AccessGranted)
            {
                scrubber.Powered = isOn;
            }
        }

        public void ApplyNumeric(object model, byte controlId, float delta, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is ScrubberInterfaceViewModel scrubber
                && controlId == MachineInterfaceControlIds.Atmos.FlowRate
                && scrubber.AccessGranted)
            {
                scrubber.FlowRate = Math.Clamp(scrubber.FlowRate + (int)delta, 1, 10);
            }
        }

        public void ApplyAction(object model, byte controlId, int value, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is not ScrubberInterfaceViewModel scrubber)
            {
                return;
            }

            if (controlId == MachineInterfaceControlIds.Atmos.ReadId)
            {
                AccessGatedOptimisticHelpers.ToggleAccessScan(scrubber);
                return;
            }

            if (controlId != MachineInterfaceControlIds.Atmos.DeviceFilter
                || !scrubber.AccessGranted
                || value < 0
                || value >= ScrubberGasFilters.KeyCount)
            {
                return;
            }

            string key = ScrubberGasFilters.Keys[value];
            scrubber.Filters ??= ScrubberGasFilters.CreateDefaultMap();
            scrubber.Filters.TryGetValue(key, out bool current);
            scrubber.Filters[key] = !current;
        }
    }

    internal sealed class VentOptimisticControlHandler : IMachineOptimisticControlHandler
    {
        public Type ViewModelType => typeof(VentInterfaceViewModel);

        public void ApplyBool(object model, byte controlId, bool isOn, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is VentInterfaceViewModel vent
                && controlId == MachineInterfaceControlIds.Atmos.Power
                && vent.AccessGranted)
            {
                vent.Powered = isOn;
            }
        }

        public void ApplyNumeric(object model, byte controlId, float delta, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is VentInterfaceViewModel vent
                && controlId == MachineInterfaceControlIds.Atmos.TargetPressure
                && vent.AccessGranted)
            {
                vent.TargetPressureKpa = Math.Clamp(vent.TargetPressureKpa + (int)delta, 0, 200);
            }
        }

        public void ApplyAction(object model, byte controlId, int value, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is VentInterfaceViewModel vent && controlId == MachineInterfaceControlIds.Atmos.ReadId)
            {
                AccessGatedOptimisticHelpers.ToggleAccessScan(vent);
            }
        }
    }

    internal sealed class PumpOptimisticControlHandler : IMachineOptimisticControlHandler
    {
        public Type ViewModelType => typeof(PumpInterfaceViewModel);

        public void ApplyBool(object model, byte controlId, bool isOn, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is PumpInterfaceViewModel pump
                && controlId == MachineInterfaceControlIds.Atmos.Power
                && pump.AccessGranted)
            {
                pump.Powered = isOn;
            }
        }

        public void ApplyNumeric(object model, byte controlId, float delta, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is PumpInterfaceViewModel pump
                && controlId == MachineInterfaceControlIds.Atmos.TargetPressure
                && pump.AccessGranted)
            {
                pump.TargetOutletPressureKpa = Math.Clamp(
                    pump.TargetOutletPressureKpa + (int)MathF.Round(delta * 100f),
                    0,
                    9000);
            }
        }

        public void ApplyAction(object model, byte controlId, int value, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is PumpInterfaceViewModel pump && controlId == MachineInterfaceControlIds.Atmos.ReadId)
            {
                AccessGatedOptimisticHelpers.ToggleAccessScan(pump);
            }
        }
    }

    internal sealed class AirAlarmOptimisticControlHandler : IMachineOptimisticControlHandler
    {
        public Type ViewModelType => typeof(AirAlarmInterfaceViewModel);

        public void ApplyBool(object model, byte controlId, bool isOn, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is AirAlarmInterfaceViewModel airAlarm)
            {
                AirAlarmInterfaceInteractionLogic.ApplyBool(airAlarm, controlId, isOn);
            }
        }

        public void ApplyNumeric(object model, byte controlId, float delta, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is AirAlarmInterfaceViewModel airAlarm)
            {
                AirAlarmInterfaceInteractionLogic.ApplyNumeric(airAlarm, controlId, delta);
            }
        }

        public void ApplyAction(object model, byte controlId, int value, MachineOptimisticControlCallbacks callbacks)
        {
            if (model is not AirAlarmInterfaceViewModel airAlarm)
            {
                return;
            }

            if (controlId == MachineInterfaceControlIds.Atmos.ReadId)
            {
                if (airAlarm.AccessGranted)
                {
                    airAlarm.AccessGranted = false;
                    airAlarm.AccessScanning = false;
                    airAlarm.AccessDenied = false;
                    airAlarm.SelectedDeviceId = null;
                }
                else if (!airAlarm.AccessScanning)
                {
                    airAlarm.AccessScanning = true;
                    airAlarm.AccessDenied = false;
                }

                return;
            }

            AirAlarmInterfaceInteractionLogic.ApplyAction(airAlarm, controlId, value);
        }
    }
}
