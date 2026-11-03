using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Tests
{
    /// <summary>
    /// Air in the test room's two rooms. A fresh room must hold standard station air (guards round
    /// start/stop atmos reset); between scenarios, breathing may add a little CO₂, anything else is a leak.
    /// </summary>
    public static class ScenarioAtmos
    {
        /// <summary>Interior cells of the test room fixture (x, z), matching Testing/fixtures/stations/testroom.json.</summary>
        public static readonly RectInt MainRoom = new(-2, -3, 5, 5);
        public static readonly RectInt SideRoom = new(4, -3, 3, 5);

        private const float StationOxygenFraction =
            AtmosConstants.StationOxygenMoles / (AtmosConstants.StationOxygenMoles + AtmosConstants.StationNitrogenMoles);

        public readonly struct Reading
        {
            public readonly int Cells;
            public readonly float MaxPressureDeviationKpa;
            public readonly float MaxTemperatureDeviationK;
            public readonly float MaxOxygenDeviation;
            public readonly float MaxCarbonDioxide;
            public readonly float MaxPlasma;

            public Reading(int cells, float pressure, float temperature, float oxygen, float carbonDioxide, float plasma)
            {
                Cells = cells;
                MaxPressureDeviationKpa = pressure;
                MaxTemperatureDeviationK = temperature;
                MaxOxygenDeviation = oxygen;
                MaxCarbonDioxide = carbonDioxide;
                MaxPlasma = plasma;
            }

            public override string ToString() =>
                $"{Cells} cells; max deviation from station air: pressure {MaxPressureDeviationKpa:F2} kPa, "
                + $"temperature {MaxTemperatureDeviationK:F2} K, O₂ {MaxOxygenDeviation:F4}; max CO₂ {MaxCarbonDioxide:F4}, plasma {MaxPlasma:F4}";
        }

        /// <summary>Fails unless both rooms hold standard station air, as a freshly loaded template must.</summary>
        public static void AssertStationAir(string when)
        {
            Reading air = Read();
            Assert.IsTrue(air.MaxPressureDeviationKpa <= 0.5f && air.MaxTemperatureDeviationK <= 0.5f
                && air.MaxOxygenDeviation <= 0.002f && air.MaxCarbonDioxide <= 0.001f && air.MaxPlasma <= 0.0001f,
                $"Room air is standard station air {when}: {air}");
        }

        /// <summary>
        /// After a scenario that did not declare it changes the room: fails on anything breathing cannot
        /// explain, and returns true when breathing has added enough CO₂ that the room should be reloaded.
        /// </summary>
        public static bool CheckUnchanged(string scenario)
        {
            Reading air = Read();
            Assert.IsTrue(air.MaxPressureDeviationKpa <= 2f && air.MaxTemperatureDeviationK <= 2f
                && air.MaxOxygenDeviation <= 0.02f && air.MaxPlasma <= 0.0001f,
                $"{scenario} changed the room's air without setting ChangesRoom: {air}");
            return air.MaxCarbonDioxide > 0.005f;
        }

        /// <summary>Worst deviation from standard station air over every interior cell of both rooms.</summary>
        public static Reading Read()
        {
            AtmosSimulation simulation = SubSystems.Get<AtmosSubSystem>().Simulation;
            TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
            Assert.IsNotNull(simulation, "Atmos is running");
            Assert.IsNotNull(map, "A station map is loaded");

            List<AtmosAreaSample> samples = new();
            foreach (RectInt room in new[] { MainRoom, SideRoom })
            foreach (Vector2Int cell in room.allPositionsWithin)
            {
                Assert.IsTrue(AtmosAreaSampler.TrySampleTile(new TileCoord(map.MapId, cell), simulation, out AtmosAreaSample sample),
                    $"Atmos has gas at room cell {cell}");
                samples.Add(sample);
            }

            return new Reading(samples.Count,
                samples.Max(s => Mathf.Abs(s.AveragePressureKpa - AtmosConstants.StandardPressure)),
                samples.Max(s => Mathf.Abs(s.TemperatureKelvin - AtmosConstants.StandardTemperature)),
                samples.Max(s => Mathf.Abs(s.OxygenMoleFraction - StationOxygenFraction)),
                samples.Max(s => s.CarbonDioxideMoleFraction),
                samples.Max(s => s.PlasmaMoleFraction));
        }
    }
}
