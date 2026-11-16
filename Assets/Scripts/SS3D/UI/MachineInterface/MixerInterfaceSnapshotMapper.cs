using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    public static class MixerInterfaceSnapshotMapper
    {
        public static MixerInterfaceViewModel ToViewModel(MixerInterfaceSnapshot snapshot)
        {
            var scenario = (MixerScenario)snapshot.Scenario;
            int inlet1Percent = snapshot.Inlet1Percent;
            var model = new MixerInterfaceViewModel
            {
                Scenario = scenario,
                ModelLabel = snapshot.ModelLabel,
                DeviceTitle = snapshot.DeviceTitle,
                Subtitle = snapshot.Subtitle,
                ChassisPowerOk = snapshot.PowerOk && scenario != MixerScenario.Stopped,
                Powered = snapshot.Powered,
                AccessGranted = snapshot.AccessGranted,
                AccessScanning = snapshot.AccessScanning,
                AccessDenied = snapshot.AccessDenied,
                Inlet1Percent = inlet1Percent,
                TargetOutletPressureKpa = snapshot.TargetOutletPressureKpa,
                OutletTargetText = $"Target {snapshot.TargetOutletPressureKpa} kPa",
                FooterText = $"SS3D Atmospherics — Mixer Unit Model {snapshot.DeviceTitle}",
            };

            ApplyStatus(model, snapshot, scenario);
            MapInlet(model.Inlet1, snapshot, snapshot.Inlet1, 1, inlet1Percent, snapshot.Inlet1Dry);
            MapInlet(model.Inlet2, snapshot, snapshot.Inlet2, 2, 100 - inlet1Percent, snapshot.Inlet2Dry);

            model.Outlet.PressureText = $"{snapshot.Outlet.PressureKpa:F1} kPa";
            model.Outlet.MixText = NonEmpty(
                GasMixDescriber.Describe(snapshot.Gases, snapshot.Outlet.Fractions, model.Outlet.Segments),
                "No gas");

            model.Inlet1Color = LeadColor(model.Inlet1.Segments, model.Inlet1Color);
            model.Inlet2Color = LeadColor(model.Inlet2.Segments, model.Inlet2Color);
            return model;
        }

        private static void ApplyStatus(
            MixerInterfaceViewModel model,
            in MixerInterfaceSnapshot snapshot,
            MixerScenario scenario)
        {
            switch (scenario)
            {
                case MixerScenario.Mixing:
                    model.StatusBadgeText = "MIXING";
                    model.StatusHeadline = "Mixing Normally";
                    model.StatusSubline = "Outlet held at target pressure.";
                    model.FlowGlyph = "→";
                    model.FlowTone = StatusTone.Success;
                    break;
                case MixerScenario.Stopped:
                    model.StatusBadgeText = "STOPPED";
                    model.StatusHeadline = GetDryHeadline(snapshot);
                    model.StatusSubline = "Mixer stopped — ratio cannot be held.";
                    model.FlowGlyph = "✕";
                    model.FlowTone = StatusTone.Warning;
                    break;
                default:
                    model.StatusBadgeText = "IDLE";
                    model.StatusHeadline = "Mixer Idle";
                    model.StatusSubline = GetIdleSubline(snapshot);
                    break;
            }

            if (!snapshot.Connected)
            {
                model.ConnectionStatus = "NO PIPE CONNECTION";
            }
        }

        private static string GetDryHeadline(in MixerInterfaceSnapshot snapshot)
        {
            if (snapshot.Inlet1Dry && snapshot.Inlet2Dry)
            {
                return "Inlets Dry";
            }

            return snapshot.Inlet1Dry ? "Inlet 1 Dry" : "Inlet 2 Dry";
        }

        private static string GetIdleSubline(in MixerInterfaceSnapshot snapshot)
        {
            if (!snapshot.PowerOk)
            {
                return "No power — no throughput.";
            }

            if (!snapshot.Powered)
            {
                return "Powered off — no throughput.";
            }

            return snapshot.Connected ? "Outlet at or above target — mixer holding." : "Awaiting pipe network link.";
        }

        private static void MapInlet(
            GasPortView port,
            in MixerInterfaceSnapshot snapshot,
            in GasLineReading reading,
            int number,
            int percent,
            bool dry)
        {
            port.Label = $"Inlet {number} · {percent}%";
            port.PressureText = $"{reading.PressureKpa:F1} kPa";
            port.Warn = dry;
            port.PressureTone = dry ? StatusTone.Warning : StatusTone.Neutral;
            string mix = GasMixDescriber.Describe(snapshot.Gases, reading.Fractions, port.Segments);
            if (dry)
            {
                port.Segments.Clear();
            }

            port.MixText = dry ? "Dry — no supply" : NonEmpty(mix, "No gas");
            port.MixTone = dry ? StatusTone.Warning : StatusTone.Neutral;
        }

        private static Color LeadColor(List<GasMixSegment> segments, Color fallback) =>
            segments.Count > 0 ? segments[0].Color : fallback;

        private static string NonEmpty(string text, string fallback) =>
            string.IsNullOrEmpty(text) ? fallback : text;
    }
}
