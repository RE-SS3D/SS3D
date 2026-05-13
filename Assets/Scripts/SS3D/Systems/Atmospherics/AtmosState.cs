// ============================================================================
// File:        AtmosState.cs
// Project:     RE-SS3D/SS3D — Issue #1464: Resurrect the Atmospherics System
// Author:      Bounty Contributor
// Description: Serializable atmospheric state for each tile, replacing the
//              previous hardcoded "air" initialization with proper tilemap
//              integration and serialization support.
// ============================================================================

using System;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Enumeration of all supported gas types in the atmospheric simulation.
    /// </summary>
    public enum GasType
    {
        Oxygen = 0,
        Nitrogen = 1,
        CarbonDioxide = 2,
        Plasma = 3,
        // Extend as needed
        Count
    }

    /// <summary>
    /// Physical constants used in atmospheric calculations.
    /// </summary>
    public static class GasConstants
    {
        /// <summary>Universal gas constant R in J/(mol·K)</summary>
        public const float R = 8.314f;

        /// <summary>Standard temperature in Kelvin (20°C)</summary>
        public const float StandardTemperature = 293.15f;

        /// <summary>Standard pressure in kPa</summary>
        public const float StandardPressure = 101.325f;

        /// <summary>Minimum pressure difference to trigger equalization (kPa)</summary>
        public const float EqualizationThreshold = 0.1f;

        /// <summary>Minimum mole count before gas is considered absent</summary>
        public const float MinimumMoles = 0.001f;

        /// <summary>
        /// Specific heat capacity at constant volume for each gas type (J/(mol·K)).
        /// Used when adding/removing heat from the atmosphere.
        /// </summary>
        public static readonly float[] SpecificHeat = new float[(int)GasType.Count]
        {
            21.1f,  // Oxygen (O₂)
            20.8f,  // Nitrogen (N₂)
            28.5f,  // Carbon Dioxide (CO₂)
            30.0f,  // Plasma (fictional)
        };

        /// <summary>
        /// Standard atmospheric composition (moles per tile at standard conditions).
        /// ~21% O₂, ~79% N₂
        /// </summary>
        public static AtmosState StandardAir(float volume = 2.5f)
        {
            var state = new AtmosState(volume);
            state.SetMoles(GasType.Oxygen, 0.21f * 100f);
            state.SetMoles(GasType.Nitrogen, 0.79f * 100f);
            state.Temperature = StandardTemperature;
            return state;
        }

        /// <summary>Returns a vacuum (empty) state.</summary>
        public static AtmosState Vacuum(float volume = 2.5f)
        {
            return new AtmosState(volume);
        }
    }

    /// <summary>
    /// Represents the atmospheric state of a single tile.
    /// Fully serializable for integration with the tilemap save/load system.
    ///
    /// BUG FIX #1 (from original PR #367):
    ///   Previously all tiles were hardcoded to "air" on first run.
    ///   Now AtmosState is serializable and integrates with tilemap persistence.
    /// </summary>
    [Serializable]
    public struct AtmosState : IEquatable<AtmosState>
    {
        [SerializeField] private float[] _gasMoles;
        [SerializeField] private float _temperature;
        [SerializeField] private float _volume;
        [SerializeField] private bool _isBlocked;
        [SerializeField] private bool _isVacuum;

        /// <summary>Temperature in Kelvin.</summary>
        public float Temperature
        {
            get => _temperature;
            set => _temperature = Mathf.Max(0f, value);
        }

        /// <summary>Volume of the tile in cubic meters.</summary>
        public float Volume
        {
            get => _volume;
            set => _volume = Mathf.Max(0.001f, value);
        }

        /// <summary>Whether this tile blocks gas flow (wall, closed airlock).</summary>
        public bool IsBlocked
        {
            get => _isBlocked;
            set => _isBlocked = value;
        }

        /// <summary>
        /// Whether this tile represents a vacuum (space).
        ///
        /// BUG FIX #3 (from original PR #367):
        ///   Vacuum tiles previously caused "weird behaviour" for neighbors.
        ///   Now vacuum is an explicit state with special handling in equalization.
        /// </summary>
        public bool IsVacuum
        {
            get => _isVacuum;
            set => _isVacuum = value;
        }

        /// <summary>
        /// Total pressure in kPa, calculated via Ideal Gas Law: P = nRT / V
        /// </summary>
        public float TotalPressure
        {
            get
            {
                if (_isVacuum || _volume <= 0f) return 0f;
                float totalMoles = TotalMoles;
                if (totalMoles <= 0f) return 0f;
                return totalMoles * GasConstants.R * _temperature / _volume;
            }
        }

        /// <summary>Total moles of all gases combined.</summary>
        public float TotalMoles
        {
            get
            {
                if (_gasMoles == null) return 0f;
                float total = 0f;
                for (int i = 0; i < _gasMoles.Length; i++)
                    total += _gasMoles[i];
                return total;
            }
        }

        public AtmosState(float volume = 2.5f)
        {
            _gasMoles = new float[(int)GasType.Count];
            _temperature = 0f;
            _volume = Mathf.Max(0.001f, volume);
            _isBlocked = false;
            _isVacuum = false;
        }

        /// <summary>Get moles of a specific gas type.</summary>
        public float GetMoles(GasType gas)
        {
            EnsureArray();
            return _gasMoles[(int)gas];
        }

        /// <summary>Set moles of a specific gas type.</summary>
        public void SetMoles(GasType gas, float moles)
        {
            EnsureArray();
            _gasMoles[(int)gas] = Mathf.Max(0f, moles);
        }

        /// <summary>Add moles of a specific gas type.</summary>
        public void AddMoles(GasType gas, float moles)
        {
            EnsureArray();
            _gasMoles[(int)gas] = Mathf.Max(0f, _gasMoles[(int)gas] + moles);
        }

        /// <summary>Remove moles of a specific gas, clamped to 0.</summary>
        public void RemoveMoles(GasType gas, float moles)
        {
            EnsureArray();
            _gasMoles[(int)gas] = Mathf.Max(0f, _gasMoles[(int)gas] - moles);
        }

        /// <summary>Get partial pressure of a specific gas.</summary>
        public float GetPartialPressure(GasType gas)
        {
            float totalMoles = TotalMoles;
            if (totalMoles <= 0f) return 0f;
            return TotalPressure * (GetMoles(gas) / totalMoles);
        }

        /// <summary>
        /// Check if the atmosphere is breathable (sufficient O₂, no toxic gases).
        /// </summary>
        public bool IsBreathable()
        {
            float o2Partial = GetPartialPressure(GasType.Oxygen);
            float co2Partial = GetPartialPressure(GasType.CarbonDioxide);
            float plasmaPartial = GetPartialPressure(GasType.Plasma);

            // O₂ partial pressure between 16 kPa and 50 kPa
            // CO₂ below 5 kPa, no plasma
            return o2Partial >= 16f && o2Partial <= 50f
                && co2Partial < 5f
                && plasmaPartial < GasConstants.MinimumMoles;
        }

        /// <summary>
        /// Check if the atmosphere is combustible (sufficient O₂ + Plasma).
        /// </summary>
        public bool IsCombustible()
        {
            float o2 = GetMoles(GasType.Oxygen);
            float plasma = GetMoles(GasType.Plasma);
            return o2 > 1f && plasma > 1f;
        }

        /// <summary>
        /// Add heat energy (Joules) to the atmosphere, taking specific heat
        /// of each gas component into account.
        ///
        /// BUG FIX (from original PR #367 commit 616cba1):
        ///   "Fix remove temperature bug" — ensures negative energy doesn't
        ///   reduce temperature below absolute zero.
        /// </summary>
        public void AddHeat(float joules)
        {
            float totalHeatCapacity = GetTotalHeatCapacity();
            if (totalHeatCapacity <= 0f) return;

            float deltaT = joules / totalHeatCapacity;
            _temperature = Mathf.Max(0f, _temperature + deltaT);
        }

        /// <summary>
        /// Total heat capacity of this gas mixture (J/K).
        /// Sum of (moles * specific_heat) for each gas component.
        /// </summary>
        public float GetTotalHeatCapacity()
        {
            EnsureArray();
            float capacity = 0f;
            for (int i = 0; i < (int)GasType.Count; i++)
            {
                capacity += _gasMoles[i] * GasConstants.SpecificHeat[i];
            }
            return capacity;
        }

        /// <summary>Reset to empty (vacuum-like but not flagged as vacuum).</summary>
        public void Clear()
        {
            EnsureArray();
            for (int i = 0; i < _gasMoles.Length; i++)
                _gasMoles[i] = 0f;
            _temperature = 0f;
        }

        private void EnsureArray()
        {
            if (_gasMoles == null || _gasMoles.Length != (int)GasType.Count)
                _gasMoles = new float[(int)GasType.Count];
        }

        public bool Equals(AtmosState other)
        {
            if (_temperature != other._temperature) return false;
            if (_volume != other._volume) return false;
            if (_isBlocked != other._isBlocked) return false;
            if (_isVacuum != other._isVacuum) return false;
            if (_gasMoles == null && other._gasMoles == null) return true;
            if (_gasMoles == null || other._gasMoles == null) return false;
            return _gasMoles.SequenceEqual(other._gasMoles);
        }

        public override bool Equals(object obj) => obj is AtmosState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_temperature, _volume, TotalMoles);
    }
}
