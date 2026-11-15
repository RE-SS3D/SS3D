using System;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>
    /// The systems views' small bar under the left toolbar, above the legend (creative-mode §4; Documents/architecture/
    /// 2026-11_map-editor-systems-views.md Phase 5): the Atmos view's Room air sub-toggle, and for admins and the host the
    /// Pause / Resume and Step of the server's atmospherics and power. Everyone in the editor sees a "Paused" tag while
    /// an admin holds the simulations; a player who may not pause sees no controls at all (the server refuses them anyway).
    /// Shown while a view is on, and while paused.
    /// </summary>
    public sealed class MapEditorSystemsSimBar
    {
        public const string PausedWords = "Atmospherics and power are paused for everyone on the server.";

        private VisualElement _bar;
        private Button _roomAir;
        private Button _pause;
        private Button _step;
        private Label _paused;
        // The view model's word at the last refresh: whether the button means Resume.
        private bool _simulationPaused;

        public event Action RoomAirToggled;
        public event Action<SystemsViewSimulationAction> SimulationRequested;

        public void Build(VisualElement column)
        {
            _bar = new VisualElement { name = "systems-sim", pickingMode = PickingMode.Position };
            _bar.AddToClassList("map-editor-systems-sim");
            _bar.style.display = DisplayStyle.None;

            _roomAir = Button("systems-room-air", "Room air", "Tint floors by pressure", () => RoomAirToggled?.Invoke());
            _roomAir.style.marginLeft = 0;
            _paused = new Label("PAUSED") { name = "systems-sim-paused", tooltip = PausedWords, pickingMode = PickingMode.Position };
            _paused.AddToClassList("map-editor-systems-sim__paused");
            VisualElement spacer = new() { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("map-editor-systems-sim__spacer");
            _pause = Button("systems-sim-pause", "Pause", "Pause atmospherics and power for everyone", () =>
                SimulationRequested?.Invoke(_simulationPaused ? SystemsViewSimulationAction.Resume : SystemsViewSimulationAction.Pause));
            _step = Button("systems-sim-step", "Step", "Run atmospherics and power one tick", () =>
                SimulationRequested?.Invoke(SystemsViewSimulationAction.Step));

            _bar.Add(_roomAir);
            _bar.Add(_paused);
            _bar.Add(spacer);
            _bar.Add(_pause);
            _bar.Add(_step);
            column.Add(_bar);
        }

        /// <summary>Follows the view model: which view is on, Room air, and the server's pause and the player's right to it.</summary>
        public void Refresh(MapEditorViewModel vm)
        {
            if (_bar == null || vm == null)
                return;

            SystemsViewKind view = vm.ActiveSystemsView;
            bool paused = _simulationPaused = vm.SystemsSimulationPaused;
            bool control = vm.MaySystemsSimulationControl;
            _bar.style.display = view != SystemsViewKind.Off || paused ? DisplayStyle.Flex : DisplayStyle.None;
            _roomAir.style.display = view == SystemsViewKind.Atmos ? DisplayStyle.Flex : DisplayStyle.None;
            _roomAir.EnableInClassList("map-editor-systems-sim__button--on", vm.SystemsRoomAir);
            _paused.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
            _pause.style.display = control ? DisplayStyle.Flex : DisplayStyle.None;
            _step.style.display = control ? DisplayStyle.Flex : DisplayStyle.None;
            _pause.text = paused ? "Resume" : "Pause";
            _pause.EnableInClassList("map-editor-systems-sim__button--on", paused);
            _step.SetEnabled(paused);
        }

        private static Button Button(string name, string text, string tooltip, Action clicked)
        {
            Button button = new(clicked) { name = name, text = text, tooltip = tooltip };
            button.AddToClassList("map-editor-systems-sim__button");
            return button;
        }
    }
}
