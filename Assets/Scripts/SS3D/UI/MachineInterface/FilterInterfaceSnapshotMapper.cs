using SS3D.Systems.Atmospherics.Pipes;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    public static class FilterInterfaceSnapshotMapper
    {
        public static FilterInterfaceViewModel ToViewModel(FilterInterfaceSnapshot snapshot)
        {
            var scenario = (FilterScenario)snapshot.Scenario;
            bool full = snapshot.FilterOutletPressureKpa >= AtmosPortConstants.FilterOutletLimitKpa;
            var model = new FilterInterfaceViewModel
            {
                Scenario = scenario,
                ModelLabel = snapshot.ModelLabel,
                DeviceTitle = snapshot.DeviceTitle,
                Subtitle = snapshot.Subtitle,
                ChassisPowerOk = snapshot.PowerOk && scenario != FilterScenario.Stalled,
                Powered = snapshot.Powered,
                AccessGranted = snapshot.AccessGranted,
                AccessScanning = snapshot.AccessScanning,
                AccessDenied = snapshot.AccessDenied,
                FlowLitresPerSecond = snapshot.FlowLitresPerSecond,
                FooterText = $"SS3D Atmospherics — Filter Unit Model {snapshot.DeviceTitle}",
                ThroughputText = $"{Mathf.RoundToInt(snapshot.ThroughputLitresPerSecond)} L/s",
            };

            ApplyStatus(model, snapshot, scenario);
            MapPorts(model, snapshot, full);
            MapGases(model, snapshot);
            return model;
        }

        private static void ApplyStatus(
            FilterInterfaceViewModel model,
            in FilterInterfaceSnapshot snapshot,
            FilterScenario scenario)
        {
            switch (scenario)
            {
                case FilterScenario.Filtering:
                    model.StatusBadgeText = "FILTERING";
                    model.StatusHeadline = "Filtering Normally";
                    model.StatusSubline = "Selected gases diverted to filter outlet.";
                    model.FlowGlyph = "→";
                    model.FlowTone = StatusTone.Success;
                    break;
                case FilterScenario.Stalled:
                    model.StatusBadgeText = "STALLED";
                    model.StatusHeadline = "Filter Stalled";
                    model.StatusSubline = snapshot.FilterOutletPressureKpa >= AtmosPortConstants.FilterOutletLimitKpa
                        ? "Filter outlet full — empty or reroute to resume."
                        : "Outlet full — empty or reroute to resume.";
                    model.FlowGlyph = "✕";
                    model.FlowTone = StatusTone.Warning;
                    break;
                default:
                    model.StatusBadgeText = "IDLE";
                    model.StatusHeadline = "Filter Idle";
                    model.StatusSubline = GetIdleSubline(snapshot);
                    break;
            }

            if (!snapshot.Connected)
            {
                model.ConnectionStatus = "NO PIPE CONNECTION";
            }
        }

        private static string GetIdleSubline(in FilterInterfaceSnapshot snapshot)
        {
            if (!snapshot.PowerOk)
            {
                return "No power — no throughput.";
            }

            if (!snapshot.Powered)
            {
                return "Powered off — no throughput.";
            }

            return snapshot.Connected ? "Inlet empty — nothing to filter." : "Awaiting pipe network link.";
        }

        private static void MapPorts(FilterInterfaceViewModel model, in FilterInterfaceSnapshot snapshot, bool full)
        {
            ulong selected = snapshot.SelectedGases;
            float[] fractions = snapshot.Inlet.Fractions;

            model.Inlet.PressureText = $"{snapshot.Inlet.PressureKpa:F1} kPa";
            model.Inlet.PressureTone = StatusTone.Neutral;
            model.Inlet.MixText = NonEmpty(GasMixDescriber.Describe(snapshot.Gases, fractions, model.Inlet.Segments), "No gas");

            model.Outlet.PressureText = $"{snapshot.OutletPressureKpa:F1} kPa";
            model.Outlet.Warn = snapshot.OutletPressureKpa >= AtmosPortConstants.FilterOutletLimitKpa;
            model.Outlet.MixText = NonEmpty(
                GasMixDescriber.Describe(snapshot.Gases, fractions, model.Outlet.Segments, row => !IsSelected(selected, row.Id)),
                "No gas");

            float capacity = snapshot.FilterOutletPressureKpa / AtmosPortConstants.FilterOutletLimitKpa;
            model.FilterOutlet.PressureText = $"{snapshot.FilterOutletPressureKpa:F1} kPa";
            model.FilterOutlet.Warn = full;
            model.FilterOutlet.MixText = NonEmpty(
                GasMixDescriber.Describe(snapshot.Gases, fractions, model.FilterOutlet.Segments, row => IsSelected(selected, row.Id)),
                "No gas selected");
            model.FilterOutlet.CapacityPercent = Mathf.Clamp01(capacity) * 100f;
            model.FilterOutlet.CapacityFull = full;
            model.FilterOutlet.CapacityText = $"{Mathf.RoundToInt(capacity * 100f)}%{(full ? " · FULL" : string.Empty)}";
        }

        private static void MapGases(FilterInterfaceViewModel model, in FilterInterfaceSnapshot snapshot)
        {
            if (snapshot.Gases == null)
            {
                return;
            }

            int selectedCount = 0;
            for (int i = 0; i < snapshot.Gases.Length; i++)
            {
                GasPanelRow gas = snapshot.Gases[i];
                float fraction = snapshot.Inlet.Fractions != null && i < snapshot.Inlet.Fractions.Length
                    ? snapshot.Inlet.Fractions[i]
                    : 0f;
                bool isSelected = IsSelected(snapshot.SelectedGases, gas.Id);
                selectedCount += isSelected ? 1 : 0;
                model.Gases.Add(new GasFilterRowView
                {
                    Id = gas.Id,
                    Symbol = gas.Symbol,
                    Name = gas.Name,
                    Color = gas.Color,
                    Selected = isSelected,
                    PercentText = $"{fraction * 100f:F1}%",
                    Present = fraction > 0f,
                });
            }

            string filtered = selectedCount > 0 ? $"{selectedCount} filtered" : "none filtered";
            model.RegistryText = $"{snapshot.Gases.Length} types · {filtered}";
        }

        private static bool IsSelected(ulong selected, byte gasId) => (selected & (1UL << gasId)) != 0UL;

        private static string NonEmpty(string text, string fallback) =>
            string.IsNullOrEmpty(text) ? fallback : text;
    }
}
