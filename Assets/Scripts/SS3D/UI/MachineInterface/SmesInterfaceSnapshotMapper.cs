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
                AccessGranted = snapshot.AccessGranted,
                AccessScanning = snapshot.AccessScanning,
            };

            ApplyStatusCopy(model);
            return model;
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
                    model.ExteriorStatusWord = "On Reserve";
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
