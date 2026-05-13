// ============================================================================
// File:        AtmosSimulation.cs
// Project:     RE-SS3D/SS3D — Issue #1464
// Description: Core atmos simulation with PV=nRT, vacuum fix, wind vector fix
// ============================================================================
using System;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public interface IAtmosSystem
    {
        void Initialize(int width, int height);
        void Step(float deltaTime);
        ref AtmosState GetState(Vector2Int position);
        void SetState(Vector2Int position, AtmosState state);
        void SetBlocked(Vector2Int position, bool blocked);
        bool IsInBounds(Vector2Int position);
    }

    public class AtmosSimulation : IAtmosSystem
    {
        private AtmosState[,] _grid;
        private bool[,] _activeTiles;
        private Vector2[,] _windVectors;
        private int _width, _height;
        private const float WIND_THRESHOLD = 5.0f;
        private const float TRANSFER_RATE = 0.4f;
        private const float TEMP_EQ_RATE = 0.2f;

        private static readonly Vector2Int[] Neighbors = {
            new(0, 1), new(0, -1), new(1, 0), new(-1, 0)
        };

        public void Initialize(int width, int height)
        {
            _width = width; _height = height;
            _grid = new AtmosState[width, height];
            _activeTiles = new bool[width, height];
            _windVectors = new Vector2[width, height];
            // Tiles start empty - tilemap integration calls SetState()
        }

        public void SetState(Vector2Int pos, AtmosState state)
        {
            if (!IsInBounds(pos)) return;
            _grid[pos.x, pos.y] = state;
            _activeTiles[pos.x, pos.y] = true;
        }

        public ref AtmosState GetState(Vector2Int pos) => ref _grid[pos.x, pos.y];

        public void SetBlocked(Vector2Int pos, bool blocked)
        {
            if (!IsInBounds(pos)) return;
            _grid[pos.x, pos.y].IsBlocked = blocked;
            ActivateArea(pos);
        }

        public bool IsInBounds(Vector2Int p) =>
            p.x >= 0 && p.x < _width && p.y >= 0 && p.y < _height;

        public void Step(float deltaTime)
        {
            for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
            {
                if (!_activeTiles[x, y] || _grid[x, y].IsBlocked) continue;
                ProcessTile(new Vector2Int(x, y));
            }
            for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
            {
                if (_activeTiles[x, y]) CheckDeactivation(new Vector2Int(x, y));
            }
        }

        private void ProcessTile(Vector2Int pos)
        {
            ref AtmosState cur = ref _grid[pos.x, pos.y];
            Vector2 wind = Vector2.zero;
            foreach (var off in Neighbors)
            {
                var np = pos + off;
                if (!IsInBounds(np)) continue;
                ref AtmosState nb = ref _grid[np.x, np.y];
                if (nb.IsBlocked) continue;

                // BUG FIX #3: Vacuum tiles - capped outflow prevents instability
                if (nb.IsVacuum) { VacuumExchange(ref cur); wind += (Vector2)off * cur.TotalPressure; continue; }
                if (cur.IsVacuum) { VacuumExchange(ref nb); continue; }

                EqualizePressure(ref cur, ref nb);
                EqualizeTemp(ref cur, ref nb);

                // BUG FIX #4: Wind FROM high TO low pressure (was reversed)
                float pd = cur.TotalPressure - nb.TotalPressure;
                if (Mathf.Abs(pd) > WIND_THRESHOLD) wind += (Vector2)off * pd;
            }
            _windVectors[pos.x, pos.y] = wind;
        }

        private void EqualizePressure(ref AtmosState a, ref AtmosState b)
        {
            if (Mathf.Abs(a.TotalPressure - b.TotalPressure) < GasConstants.EqualizationThreshold) return;
            for (int i = 0; i < (int)GasType.Count; i++)
            {
                float total = a.GetMoles((GasType)i) + b.GetMoles((GasType)i);
                if (total < GasConstants.MinimumMoles) continue;
                float target = total * 0.5f;
                float delta = (a.GetMoles((GasType)i) - target) * TRANSFER_RATE;
                a.AddMoles((GasType)i, -delta);
                b.AddMoles((GasType)i, delta);
            }
        }

        private void EqualizeTemp(ref AtmosState a, ref AtmosState b)
        {
            if (Mathf.Abs(a.Temperature - b.Temperature) < 0.1f) return;
            float cA = a.GetTotalHeatCapacity(), cB = b.GetTotalHeatCapacity();
            if (cA + cB <= 0) return;
            float target = (a.Temperature * cA + b.Temperature * cB) / (cA + cB);
            a.Temperature = Mathf.Lerp(a.Temperature, target, TEMP_EQ_RATE);
            b.Temperature = Mathf.Lerp(b.Temperature, target, TEMP_EQ_RATE);
        }

        private void VacuumExchange(ref AtmosState src)
        {
            const float rate = 0.10f;
            for (int i = 0; i < (int)GasType.Count; i++)
                if (src.GetMoles((GasType)i) > GasConstants.MinimumMoles)
                    src.RemoveMoles((GasType)i, src.GetMoles((GasType)i) * rate);
            src.Temperature = Mathf.Max(0, src.Temperature * (1 - rate * 0.5f));
        }

        private void CheckDeactivation(Vector2Int pos)
        {
            ref AtmosState cur = ref _grid[pos.x, pos.y];
            foreach (var off in Neighbors)
            {
                var np = pos + off;
                if (!IsInBounds(np)) continue;
                ref AtmosState nb = ref _grid[np.x, np.y];
                if (nb.IsBlocked) continue;
                if (nb.IsVacuum) return;
                if (Mathf.Abs(cur.TotalPressure - nb.TotalPressure) > GasConstants.EqualizationThreshold) return;
                if (Mathf.Abs(cur.Temperature - nb.Temperature) > 0.5f) return;
            }
            _activeTiles[pos.x, pos.y] = false;
            _windVectors[pos.x, pos.y] = Vector2.zero;
        }

        private void ActivateArea(Vector2Int pos)
        {
            if (IsInBounds(pos)) _activeTiles[pos.x, pos.y] = true;
            foreach (var off in Neighbors) { var np = pos + off; if (IsInBounds(np)) _activeTiles[np.x, np.y] = true; }
        }

        public Vector2 GetWindVector(Vector2Int pos) => IsInBounds(pos) ? _windVectors[pos.x, pos.y] : Vector2.zero;
        public bool IsActive(Vector2Int pos) => IsInBounds(pos) && _activeTiles[pos.x, pos.y];
    }
}
