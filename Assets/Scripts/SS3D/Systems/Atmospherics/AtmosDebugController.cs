using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using SS3D.Systems.Inputs;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Atmospherics
{
    public enum AtmosDebugViewMode
    {
        Pressure,
        Temperature,
        Active,
        State,
    }

    /// <summary>
    /// Runtime atmospherics debug overlay. Toggle with P (Other/Toggle Atmos Debug).
    /// Server/host only — reads and mutates the authoritative sim.
    /// </summary>
    public sealed class AtmosDebugController : Actor
    {
        private const float DrawRadius = 40f;
        private const float ReferencePressureKpa = 101.325f;

        private InputSubSystem _inputSystem;
        private InputAction _toggleAction;
        private AtmosSubSystem _atmos;
        private TileSubSystem _tileSubSystem;
        private Camera _camera;

        private bool _open;
        private Rect _panelRect = new Rect(16f, 16f, 360f, 520f);
        private AtmosDebugViewMode _viewMode = AtmosDebugViewMode.Pressure;
        private bool _drawOverlay = true;
        private bool _showVacuum = true;
        private bool _showInactive;
        private float _gizmoScale = 1f;
        private TileCoord? _selectedCoord;
        private Vector2 _scroll;

        protected override void OnStart()
        {
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _atmos = SubSystems.Get<AtmosSubSystem>();
            _tileSubSystem = SubSystems.Get<TileSubSystem>();
            _camera = Camera.main;

            if (_inputSystem != null)
            {
                _toggleAction = _inputSystem.Inputs.FindAction("Other/Toggle Atmos Debug", throwIfNotFound: false);
                if (_toggleAction != null)
                    _toggleAction.performed += OnToggle;
            }
        }

        protected override void OnDestroyed()
        {
            if (_toggleAction != null)
                _toggleAction.performed -= OnToggle;
        }

        private void Update()
        {
            if (_toggleAction == null && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
                _open = !_open;
        }

        private void LateUpdate()
        {
            if (!_open || _atmos?.Simulation == null || _camera == null)
                return;

            DrawWorldOverlay();
            TryPickTile();
        }

        private void OnGUI()
        {
            if (!_open)
                return;

            _panelRect = GUILayout.Window(
                GetInstanceID(),
                _panelRect,
                DrawPanel,
                "Atmospherics Debug (P)");
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            _open = !_open;
            if (_open && _camera == null)
                _camera = Camera.main;
        }

        private void DrawPanel(int windowId)
        {
            _scroll = GUILayout.BeginScrollView(_scroll);

            if (_atmos == null || _atmos.Simulation == null)
            {
                GUILayout.Label("AtmosSubSystem is not running on the server.");
                GUILayout.EndScrollView();
                GUI.DragWindow();
                return;
            }

            AtmosSimulation sim = _atmos.Simulation;
            GUILayout.Label($"Cells: {sim.CellCount}  |  Active: {sim.ActiveCellCount}");
            GUILayout.Label($"Total moles: {sim.GetTotalMoles():F1}  |  Last tick: {_atmos.LastTickMilliseconds:F2} ms");
            GUILayout.Label($"Paused: {_atmos.SimulationPaused}  |  Tick: {AtmosConstants.TickInterval:F1}s");

            GUILayout.Space(6f);
            GUILayout.Label("View mode");
            _viewMode = (AtmosDebugViewMode)GUILayout.Toolbar((int)_viewMode, new[] { "Pressure", "Temp", "Active", "State" });

            _drawOverlay = GUILayout.Toggle(_drawOverlay, "Draw world overlay");
            _showVacuum = GUILayout.Toggle(_showVacuum, "Show vacuum");
            _showInactive = GUILayout.Toggle(_showInactive, "Show inactive");
            GUILayout.Label("Gizmo scale");
            _gizmoScale = GUILayout.HorizontalSlider(_gizmoScale, 0.25f, 3f);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_atmos.SimulationPaused ? "Resume" : "Pause"))
                _atmos.SimulationPaused = !_atmos.SimulationPaused;
            if (GUILayout.Button("Step tick"))
                _atmos.StepOnce();
            GUILayout.EndHorizontal();

            if (_selectedCoord is TileCoord selected && _atmos.TryGetCellDebugInfo(selected, out AtmosCellDebugInfo info))
                DrawSelectedCell(info);
            else
                GUILayout.Label("Click a tile in the world to inspect.");

            GUILayout.Space(6f);
            if (_selectedCoord is TileCoord spawnCoord)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Wake 3×3"))
                    _atmos.DebugWakeRegion(spawnCoord, 1);
                if (GUILayout.Button("+O₂ 10 mol"))
                    _atmos.DebugAddGas(spawnCoord, AtmosConstants.Oxygen, 10f);
                if (GUILayout.Button("+Plasma 5 mol"))
                    _atmos.DebugAddGas(spawnCoord, AtmosConstants.Plasma, 5f);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+500 K"))
                    _atmos.DebugAddHeat(spawnCoord, 500f);
                if (GUILayout.Button("Ignite (1000 K)"))
                    _atmos.DebugAddHeat(spawnCoord, 1000f);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawSelectedCell(AtmosCellDebugInfo info)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Tile {info.Coord.Grid.x}, {info.Coord.Grid.y}");
            builder.AppendLine($"State: {info.State}");
            builder.AppendLine($"Pressure: {info.Pressure:F2} kPa");
            builder.AppendLine($"Temperature: {info.Temperature:F1} K");
            builder.AppendLine($"Volume: {info.Volume:F2}");
            builder.AppendLine($"Plenum: {info.Occupancy.HasPlenum}  Airtight: {info.Occupancy.IsAirtight}");
            builder.AppendLine($"Blocked edges: {Convert.ToString(info.Occupancy.BlockedEdges, 2).PadLeft(4, '0')} (NESW)");
            builder.AppendLine($"Neighbours N/E/S/W: {info.Neighbours.North}/{info.Neighbours.East}/{info.Neighbours.South}/{info.Neighbours.West}");

            AtmosSimulation sim = _atmos?.Simulation;
            if (sim != null)
            {
                builder.AppendLine("Gas (mol):");
                builder.AppendLine($"  O₂ {sim.DebugGetMoles(info.Coord, AtmosConstants.Oxygen):F2}" +
                    $"  N₂ {sim.DebugGetMoles(info.Coord, AtmosConstants.Nitrogen):F2}");
                builder.AppendLine($"  CO₂ {sim.DebugGetMoles(info.Coord, AtmosConstants.CarbonDioxide):F2}" +
                    $"  Plasma {sim.DebugGetMoles(info.Coord, AtmosConstants.Plasma):F2}");
            }

            GUILayout.TextArea(builder.ToString());
        }

        private void TryPickTile()
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame || _tileSubSystem?.QueryService == null)
                return;

            if (_panelRect.Contains(GetGuiMousePosition()))
                return;

            Vector3 world = TileHelper.GetPointedPosition(isTilePosition: true);
            int mapId = _tileSubSystem.CurrentMap?.MapId ?? 0;
            var coord = new TileCoord(mapId, Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));
            if (_atmos.TryGetCellDebugInfo(coord, out _))
                _selectedCoord = coord;
        }

        private void DrawWorldOverlay()
        {
            if (!_drawOverlay)
                return;

            Vector3 cameraPosition = _camera.transform.position;
            float maxPressure = 0f;

            _atmos.Simulation.ForEachCoord(coord =>
            {
                if (!ShouldDrawCell(coord, cameraPosition))
                    return;

                if (_atmos.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info))
                    maxPressure = Mathf.Max(maxPressure, info.Pressure);
            });

            if (maxPressure <= 0f)
                maxPressure = ReferencePressureKpa;

            _atmos.Simulation.ForEachCoord(coord =>
            {
                if (!ShouldDrawCell(coord, cameraPosition))
                    return;

                if (!_atmos.TryGetCellDebugInfo(coord, out AtmosCellDebugInfo info))
                    return;

                if (!ShouldShowCell(info))
                    return;

                Color color = GetCellColor(info, maxPressure, _viewMode);
                float height = GetCellHeight(info, maxPressure);
                DrawCellCube(coord, height * _gizmoScale, color);
            });
        }

        private bool ShouldDrawCell(TileCoord coord, Vector3 cameraPosition)
        {
            float dx = coord.Grid.x - cameraPosition.x;
            float dz = coord.Grid.y - cameraPosition.z;
            return dx * dx + dz * dz <= DrawRadius * DrawRadius;
        }

        private bool ShouldShowCell(AtmosCellDebugInfo info)
        {
            if (info.State == AtmosCellState.Vacuum)
                return _showVacuum;

            if (info.State == AtmosCellState.Inactive || info.State == AtmosCellState.Blocked)
                return _showInactive || info.Occupancy.HasPlenum;

            return info.Occupancy.HasPlenum || _showVacuum;
        }

        private static Color GetCellColor(AtmosCellDebugInfo info, float maxPressure, AtmosDebugViewMode viewMode)
        {
            if (viewMode == AtmosDebugViewMode.Temperature)
                return TemperatureColor(info.Temperature);

            if (viewMode == AtmosDebugViewMode.Active)
            {
                return info.State switch
                {
                    AtmosCellState.Active => new Color(0.2f, 1f, 0.3f, 0.9f),
                    AtmosCellState.Semiactive => new Color(0.9f, 0.8f, 0.2f, 0.85f),
                    _ => new Color(0.4f, 0.4f, 0.4f, 0.5f),
                };
            }

            if (viewMode == AtmosDebugViewMode.State)
            {
                return info.State switch
                {
                    AtmosCellState.Vacuum => new Color(0.2f, 0.2f, 0.8f, 0.8f),
                    AtmosCellState.Blocked => new Color(0.15f, 0.15f, 0.15f, 0.8f),
                    AtmosCellState.Inactive => new Color(0.5f, 0.5f, 0.5f, 0.6f),
                    AtmosCellState.Semiactive => new Color(0.9f, 0.8f, 0.2f, 0.85f),
                    _ => new Color(0.2f, 1f, 0.3f, 0.9f),
                };
            }

            return info.State switch
            {
                AtmosCellState.Vacuum => new Color(0.2f, 0.2f, 0.8f, 0.8f),
                AtmosCellState.Blocked => new Color(0.15f, 0.15f, 0.15f, 0.8f),
                _ => Color.Lerp(new Color(0.2f, 0.6f, 1f), new Color(1f, 0.2f, 0.2f), Mathf.Clamp01(info.Pressure / maxPressure)),
            };
        }

        // Multi-stop ramp so space-cold reads as a distinct near-black blue rather than looking
        // like a merely cool (but breathable) room. Room temperature is a bright cyan, then it
        // climbs orange -> red -> white-hot for fire.
        private static Color TemperatureColor(float temperature)
        {
            Color space = new Color(0.02f, 0.02f, 0.10f); // <= 173 K, near-black blue (vacuum)
            Color cold = new Color(0.10f, 0.35f, 0.85f);  // ~273 K, chilly blue
            Color room = new Color(0.15f, 0.75f, 0.85f);  // ~293 K, bright cyan
            Color warm = new Color(1f, 0.55f, 0.10f);     // ~600 K, orange
            Color hot = new Color(1f, 0.15f, 0.05f);      // ~1000 K, red
            Color blaze = new Color(1f, 1f, 0.90f);       // >= 2000 K, white-hot

            if (temperature <= 173f)
                return space;
            if (temperature < 273f)
                return Color.Lerp(space, cold, Mathf.InverseLerp(173f, 273f, temperature));
            if (temperature < 293f)
                return Color.Lerp(cold, room, Mathf.InverseLerp(273f, 293f, temperature));
            if (temperature < 600f)
                return Color.Lerp(room, warm, Mathf.InverseLerp(293f, 600f, temperature));
            if (temperature < 1000f)
                return Color.Lerp(warm, hot, Mathf.InverseLerp(600f, 1000f, temperature));

            return Color.Lerp(hot, blaze, Mathf.InverseLerp(1000f, 2000f, temperature));
        }

        private float GetCellHeight(AtmosCellDebugInfo info, float maxPressure)
        {
            return _viewMode switch
            {
                AtmosDebugViewMode.Temperature => Mathf.Lerp(0.2f, 2f, Mathf.InverseLerp(173f, 1000f, info.Temperature)),
                AtmosDebugViewMode.Active => info.State is AtmosCellState.Active or AtmosCellState.Semiactive ? 1.2f : 0.2f,
                AtmosDebugViewMode.State => 0.6f,
                _ => Mathf.Lerp(0.2f, 2f, Mathf.Clamp01(info.Pressure / maxPressure)),
            };
        }

        private static void DrawCellCube(TileCoord coord, float height, Color color)
        {
            Vector3 center = new Vector3(coord.Grid.x, height * 0.5f, coord.Grid.y);
            Vector3 size = new Vector3(0.95f, height, 0.95f);
            Vector3 half = size * 0.5f;

            Vector3 c000 = center + new Vector3(-half.x, -half.y, -half.z);
            Vector3 c001 = center + new Vector3(-half.x, -half.y, half.z);
            Vector3 c010 = center + new Vector3(-half.x, half.y, -half.z);
            Vector3 c011 = center + new Vector3(-half.x, half.y, half.z);
            Vector3 c100 = center + new Vector3(half.x, -half.y, -half.z);
            Vector3 c101 = center + new Vector3(half.x, -half.y, half.z);
            Vector3 c110 = center + new Vector3(half.x, half.y, -half.z);
            Vector3 c111 = center + new Vector3(half.x, half.y, half.z);

            DrawEdge(c000, c001, color);
            DrawEdge(c000, c010, color);
            DrawEdge(c001, c011, color);
            DrawEdge(c010, c011, color);
            DrawEdge(c100, c101, color);
            DrawEdge(c100, c110, color);
            DrawEdge(c101, c111, color);
            DrawEdge(c110, c111, color);
            DrawEdge(c000, c100, color);
            DrawEdge(c001, c101, color);
            DrawEdge(c010, c110, color);
            DrawEdge(c011, c111, color);
        }

        private static Vector2 GetGuiMousePosition()
        {
            Vector2 mouse = Mouse.current.position.ReadValue();
            mouse.y = Screen.height - mouse.y;
            return mouse;
        }

        private static void DrawEdge(Vector3 a, Vector3 b, Color color)
        {
            Debug.DrawLine(a, b, color, Time.deltaTime, false);
        }
    }
}
