namespace SS3D.UI.MachineInterface
{
    public static class GasPumpInterfaceSnapshotMapper
    {
        public static GasPumpInterfaceViewModel ToViewModel(GasPumpInterfaceSnapshot snapshot)
        {
            StatusTone tone = snapshot.HealthState switch
            {
                3 => StatusTone.Danger,
                2 => StatusTone.Warning,
                1 => StatusTone.Info,
                _ => StatusTone.Info,
            };

            string status = snapshot.HealthState switch
            {
                3 => "UNPOWERED",
                2 => "STALLED — ΔP EXCEEDS MOTOR RATING",
                1 => "TRANSFERRING",
                _ when !snapshot.Enabled => "DISABLED",
                _ when !snapshot.Connected => "NO PIPE CONNECTION",
                _ => "IDLE",
            };

            float flowPct = snapshot.RatedMaxFlowMolesPerSecond > 0f
                ? snapshot.CurrentFlowMolesPerSecond / snapshot.RatedMaxFlowMolesPerSecond
                : 0f;

            return new GasPumpInterfaceViewModel
            {
                Title = snapshot.Title,
                ModelLabel = snapshot.ModelLabel,
                PowerOk = snapshot.PowerOk,
                Enabled = snapshot.Enabled,
                Connected = snapshot.Connected,
                RatedMaxFlowMolesPerSecond = snapshot.RatedMaxFlowMolesPerSecond,
                CurrentFlowMolesPerSecond = snapshot.CurrentFlowMolesPerSecond,
                DifferentialKpa = snapshot.DifferentialKpa,
                MaxDifferentialKpa = snapshot.MaxDifferentialKpa,
                Stalled = snapshot.Stalled,
                HealthState = snapshot.HealthState,
                FlowReadout = $"{snapshot.CurrentFlowMolesPerSecond:F1} / {snapshot.RatedMaxFlowMolesPerSecond:F1} mol/s ({flowPct:P0})",
                DifferentialReadout = $"{snapshot.DifferentialKpa:F1} / {snapshot.MaxDifferentialKpa:F1} kPa",
                StatusReadout = status,
                StatusTone = tone,
            };
        }
    }
}
