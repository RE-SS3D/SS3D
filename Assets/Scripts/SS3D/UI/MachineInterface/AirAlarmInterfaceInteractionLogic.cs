using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Client-side air alarm UI state transitions (mirrors design prototype behaviour).
    /// </summary>
    public static class AirAlarmInterfaceInteractionLogic
    {
        public const int MaxConnectedDevices = 4;

        private static readonly string[] FilterKeys = { "O2", "N2", "CO2", "Plasma", "Toxins" };

        public static void ApplyAction(AirAlarmInterfaceViewModel model, byte controlId, int value)
        {
            if (model == null)
            {
                return;
            }

            switch (controlId)
            {
                case MachineInterfaceControlIds.Atmos.PresetMode:
                {
                    if (model.AccessGranted)
                    {
                        ApplyPresetMode(model, (AirAlarmPresetMode)value);
                    }

                    break;
                }

                case MachineInterfaceControlIds.Atmos.SelectDevice:
                {
                    ToggleDeviceSelection(model, value);
                    break;
                }

                case MachineInterfaceControlIds.Atmos.CloseDevice:
                {
                    model.SelectedDeviceId = null;
                    break;
                }

                case MachineInterfaceControlIds.Atmos.DeviceFilter:
                {
                    if (model.AccessGranted)
                    {
                        ToggleSelectedDeviceFilter(model, value);
                    }

                    break;
                }
            }
        }

        public static void ApplyBool(AirAlarmInterfaceViewModel model, byte controlId, bool value)
        {
            if (model == null || !model.AccessGranted)
            {
                return;
            }

            if (controlId != MachineInterfaceControlIds.Atmos.Power)
            {
                return;
            }

            AirAlarmConnectedDevice device = GetSelectedDevice(model);
            if (device != null)
            {
                device.Powered = value;
            }
        }

        public static void ApplyNumeric(AirAlarmInterfaceViewModel model, byte controlId, float delta)
        {
            if (model == null || !model.AccessGranted)
            {
                return;
            }

            if (controlId != MachineInterfaceControlIds.Atmos.TargetPressure)
            {
                return;
            }

            AirAlarmConnectedDevice device = GetSelectedDevice(model);
            if (device != null && device.Kind == AirAlarmConnectedDeviceKind.Vent)
            {
                device.TargetKpa = System.Math.Clamp(device.TargetKpa + delta, 0f, 200f);
            }
        }

        public static void ApplyPresetMode(AirAlarmInterfaceViewModel model, AirAlarmPresetMode mode)
        {
            model.ActiveMode = mode;

            foreach (AirAlarmConnectedDevice device in model.ConnectedDevices)
            {
                switch (mode)
                {
                    case AirAlarmPresetMode.Filtering:
                    {
                        device.Powered = true;
                        break;
                    }

                    case AirAlarmPresetMode.Panic:
                    {
                        if (device.Kind == AirAlarmConnectedDeviceKind.Vent)
                        {
                            device.Powered = false;
                        }
                        else
                        {
                            device.Powered = true;
                            SetScrubberFilters(device, o2: false, n2: false, co2: false, plasma: true, toxins: false);
                        }

                        break;
                    }

                    case AirAlarmPresetMode.Fill:
                    {
                        device.Powered = device.Kind == AirAlarmConnectedDeviceKind.Vent;
                        break;
                    }

                    case AirAlarmPresetMode.Off:
                    {
                        device.Powered = false;
                        break;
                    }
                }
            }
        }

        public static AirAlarmConnectedDevice CloneDevice(AirAlarmConnectedDevice source)
        {
            if (source == null)
            {
                return null;
            }

            return new AirAlarmConnectedDevice
            {
                Id = source.Id,
                Kind = source.Kind,
                Name = source.Name,
                Powered = source.Powered,
                TargetKpa = source.TargetKpa,
                Filters = source.Filters != null
                    ? new Dictionary<string, bool>(source.Filters)
                    : new Dictionary<string, bool>(),
            };
        }

        public static void CopyDevicesToModel(
            AirAlarmInterfaceViewModel model,
            IReadOnlyList<AirAlarmConnectedDevice> devices)
        {
            model.ConnectedDevices.Clear();
            if (devices == null)
            {
                return;
            }

            foreach (AirAlarmConnectedDevice device in devices)
            {
                model.ConnectedDevices.Add(CloneDevice(device));
            }
        }

        private static void ToggleDeviceSelection(AirAlarmInterfaceViewModel model, int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= model.ConnectedDevices.Count)
            {
                model.SelectedDeviceId = null;
                return;
            }

            string deviceId = model.ConnectedDevices[deviceIndex].Id;
            model.SelectedDeviceId = model.SelectedDeviceId == deviceId ? null : deviceId;
        }

        private static void ToggleSelectedDeviceFilter(AirAlarmInterfaceViewModel model, int filterIndex)
        {
            AirAlarmConnectedDevice device = GetSelectedDevice(model);
            if (device == null
                || device.Kind != AirAlarmConnectedDeviceKind.Scrubber
                || filterIndex < 0
                || filterIndex >= FilterKeys.Length)
            {
                return;
            }

            string key = FilterKeys[filterIndex];
            device.Filters ??= new Dictionary<string, bool>();
            device.Filters.TryGetValue(key, out bool current);
            device.Filters[key] = !current;
        }

        private static AirAlarmConnectedDevice GetSelectedDevice(AirAlarmInterfaceViewModel model)
        {
            if (string.IsNullOrEmpty(model.SelectedDeviceId))
            {
                return null;
            }

            foreach (AirAlarmConnectedDevice device in model.ConnectedDevices)
            {
                if (device.Id == model.SelectedDeviceId)
                {
                    return device;
                }
            }

            return null;
        }

        private static void SetScrubberFilters(
            AirAlarmConnectedDevice device,
            bool o2,
            bool n2,
            bool co2,
            bool plasma,
            bool toxins)
        {
            device.Filters ??= new Dictionary<string, bool>();
            device.Filters["O2"] = o2;
            device.Filters["N2"] = n2;
            device.Filters["CO2"] = co2;
            device.Filters["Plasma"] = plasma;
            device.Filters["Toxins"] = toxins;
        }
    }
}
