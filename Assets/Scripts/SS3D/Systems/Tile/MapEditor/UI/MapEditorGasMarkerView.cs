using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile.GasMarkers;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    public sealed partial class MapEditorView
    {
        private const string GasMarkerPopover = "gasMarker";
        private const string CustomMix = "Custom";
        private const string VacuumMix = "Vacuum";

        private static readonly (string Name, TileLayer Layer)[] GasMarkerLines =
        {
            ("Supply", TileLayer.PipeLeft),
            ("Mixed", TileLayer.PipeMiddle),
            ("Waste", TileLayer.PipeRight),
            ("Surface", TileLayer.PipeSurface),
        };

        private bool _gasMarkerEditingPlaced;

        /// <summary>The form's marker, sent when an already placed marker is applied.</summary>
        public event Action<GasMarkerRecord> GasMarkerApplyRequested;

        /// <summary>Opens the form on the view model's draft. Placing reads the draft, so it needs no Apply.</summary>
        public void ShowGasMarkerProperties(bool editingPlaced)
        {
            _gasMarkerEditingPlaced = editingPlaced;
            OpenPopover(GasMarkerPopover);
        }

        private void BuildGasMarkerPopover(VisualElement container)
        {
            container.AddToClassList("map-editor-area-form");
            GasMarkerRecord draft = _vm.GasMarkerDraft;

            Label help = new(GasMarkerHelp(draft.Kind)) { name = "gas-marker-help" };
            help.style.whiteSpace = WhiteSpace.Normal;
            container.Add(help);

            if (draft.Kind == GasMarkerKind.LineFill)
                AddGasMarkerLineField(container, draft);

            VisualElement pressureRow = new();
            VisualElement customRows = new();
            AddGasMarkerMixField(container, draft, pressureRow, customRows);
            container.Add(customRows);
            AddGasMarkerPressureField(pressureRow, draft);
            container.Add(pressureRow);

            if (!_gasMarkerEditingPlaced)
                return;

            Button apply = new(() =>
            {
                GasMarkerApplyRequested?.Invoke(_vm.GasMarkerDraft);
            }) { text = "Apply to marker", name = "gas-marker-apply" };
            apply.AddToClassList("map-editor-action-btn");
            apply.AddToClassList("map-editor-action-btn--primary");
            container.Add(apply);
        }

        private static string GasMarkerHelp(GasMarkerKind kind) => kind switch
        {
            GasMarkerKind.LineFill => "Fills the pipe network on this line of its tile, once at round start.",
            GasMarkerKind.RoomAir => "Gives the room around it this air, once at round start. Doors bound the room.",
            _ => "Holds its tile at this mix and pressure for the whole round. Use it in a sealed storage chamber.",
        };

        private void AddGasMarkerLineField(VisualElement container, GasMarkerRecord draft)
        {
            List<string> names = new();
            int selected = 0;
            for (int i = 0; i < GasMarkerLines.Length; i++)
            {
                names.Add(GasMarkerLines[i].Name);
                if (GasMarkerLines[i].Layer == draft.Line)
                    selected = i;
            }

            container.Add(new Label("Pipe line"));
            DropdownField line = new(names, selected) { name = "gas-marker-line" };
            line.AddToClassList("map-editor-area-field");
            line.RegisterValueChangedCallback(_ => UpdateGasMarkerDraft((ref GasMarkerRecord record) => record.Line = GasMarkerLines[line.index].Layer));
            container.Add(line);
        }

        private void AddGasMarkerMixField(VisualElement container, GasMarkerRecord draft, VisualElement pressureRow, VisualElement customRows)
        {
            IReadOnlyList<GasDefinition> gases = KnownGases();
            List<string> names = new() { "Air" };
            foreach (GasDefinition gas in gases)
                names.Add(gas.DisplayName);
            if (draft.Kind == GasMarkerKind.RoomAir)
                names.Add(VacuumMix);
            names.Add(CustomMix);

            string current = MapEditorGasMarkerCatalog.MixName(draft.GasFractions);
            container.Add(new Label("Gas mix"));
            DropdownField mix = new(names, Math.Max(0, names.IndexOf(current))) { name = "gas-marker-mix" };
            mix.AddToClassList("map-editor-area-field");
            container.Add(mix);

            void ShowFor(string choice)
            {
                customRows.style.display = choice == CustomMix ? DisplayStyle.Flex : DisplayStyle.None;
                pressureRow.style.display = choice == VacuumMix ? DisplayStyle.None : DisplayStyle.Flex;
            }

            mix.RegisterValueChangedCallback(evt =>
            {
                ShowFor(evt.newValue);
                float[] fractions = FractionsFor(evt.newValue, gases);
                if (fractions != null)
                    UpdateGasMarkerDraft((ref GasMarkerRecord record) => record.GasFractions = fractions);
                else
                    RebuildGasMarkerCustomRows(customRows, gases);
            });
            RebuildGasMarkerCustomRows(customRows, gases);
            ShowFor(current);
        }

        // Null for Custom, whose rows keep the mix as it stands.
        private static float[] FractionsFor(string choice, IReadOnlyList<GasDefinition> gases)
        {
            if (choice == CustomMix)
                return null;

            if (choice == VacuumMix)
                return AtmosGasMix.Vacuum();

            foreach (GasDefinition gas in gases)
            {
                if (gas.DisplayName == choice)
                    return AtmosGasMix.Pure(gas.GasId);
            }

            return AtmosGasMix.Air();
        }

        private void RebuildGasMarkerCustomRows(VisualElement rows, IReadOnlyList<GasDefinition> gases)
        {
            rows.Clear();
            float[] fractions = _vm.GasMarkerDraft.GasFractions;
            float total = 0f;
            foreach (float fraction in fractions)
                total += fraction;

            foreach (GasDefinition gas in gases)
            {
                int id = gas.Id;
                float share = id < fractions.Length && total > 0f ? fractions[id] / total * 100f : 0f;
                rows.Add(new Label($"{gas.DisplayName} (%)"));
                TextField percent = new() { name = $"gas-marker-percent-{id}", value = FormatNumber(share) };
                percent.AddToClassList("map-editor-area-field");
                WireTextEntryField(percent);
                percent.RegisterValueChangedCallback(evt =>
                {
                    if (!TryParseNumber(evt.newValue, out float value))
                        return;

                    UpdateGasMarkerDraft((ref GasMarkerRecord record) => record.GasFractions = WithFraction(record.GasFractions, id, value));
                });
                rows.Add(percent);
            }
        }

        private void AddGasMarkerPressureField(VisualElement row, GasMarkerRecord draft)
        {
            row.Add(new Label("Pressure (kPa)"));
            TextField pressure = new() { name = "gas-marker-pressure", value = FormatNumber(draft.PressureKpa) };
            pressure.AddToClassList("map-editor-area-field");
            WireTextEntryField(pressure);
            pressure.RegisterValueChangedCallback(evt =>
            {
                if (!TryParseNumber(evt.newValue, out float value))
                    return;

                UpdateGasMarkerDraft((ref GasMarkerRecord record) => record.PressureKpa = Mathf.Min(value, GasMarkerRecord.MaxPressureKpa));
            });
            row.Add(pressure);
        }

        private delegate void GasMarkerEdit(ref GasMarkerRecord record);

        private void UpdateGasMarkerDraft(GasMarkerEdit edit)
        {
            GasMarkerRecord draft = _vm.GasMarkerDraft;
            edit(ref draft);
            _vm.GasMarkerDraft = draft;
        }

        // A new array, because undo history may hold the old one.
        private static float[] WithFraction(float[] fractions, int id, float value)
        {
            var copy = new float[Math.Max(fractions.Length, id + 1)];
            fractions.CopyTo(copy, 0);
            copy[id] = value;
            return copy;
        }

        private static IReadOnlyList<GasDefinition> KnownGases() =>
            SubSystems.TryGet(out AtmosSubSystem atmos) && atmos.GasRegistry != null
                ? atmos.GasRegistry.Definitions
                : Array.Empty<GasDefinition>();

        private static string FormatNumber(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static bool TryParseNumber(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && value >= 0f && !float.IsInfinity(value);
    }
}
