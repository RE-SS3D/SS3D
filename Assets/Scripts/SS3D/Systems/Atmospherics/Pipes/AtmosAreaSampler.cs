using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    public readonly struct AtmosAreaSample
    {
        public readonly int CellCount;
        public readonly float AveragePressureKpa;
        public readonly float OxygenMoleFraction;
        public readonly float CarbonDioxideMoleFraction;

        public AtmosAreaSample(
            int cellCount,
            float averagePressureKpa,
            float oxygenMoleFraction,
            float carbonDioxideMoleFraction)
        {
            CellCount = cellCount;
            AveragePressureKpa = averagePressureKpa;
            OxygenMoleFraction = oxygenMoleFraction;
            CarbonDioxideMoleFraction = carbonDioxideMoleFraction;
        }

        public static AtmosAreaSample Empty => new(0, 0f, 0f, 0f);
    }

    /// <summary>
    /// Aggregates turf gas readings for all tiles in an area.
    /// </summary>
    public static class AtmosAreaSampler
    {
        public static bool TrySampleArea(
            TileMap map,
            ITileQueryService query,
            AreaId areaId,
            AtmosSimulation simulation,
            out AtmosAreaSample sample)
        {
            sample = AtmosAreaSample.Empty;
            if (map == null || query == null || areaId.IsNone || simulation == null)
            {
                return false;
            }

            int cellCount = 0;
            float pressureSum = 0f;
            float oxygenSum = 0f;
            float carbonDioxideSum = 0f;
            float totalMolesSum = 0f;

            foreach (TileChunk chunk in map.GetAllChunks())
            {
                if (!chunk.HasAnyAreaIds())
                {
                    continue;
                }

                for (int x = 0; x < TileChunk.ChunkSize; x++)
                {
                    for (int y = 0; y < TileChunk.ChunkSize; y++)
                    {
                        if (chunk.GetAreaId(x, y) != areaId.Value)
                        {
                            continue;
                        }

                        Vector3 world = chunk.GetWorldPosition(x, y);
                        TileCoord coord = query.WorldToTile(world, map.MapId);
                        if (!simulation.TryGetCellDebugInfo(coord, out _))
                        {
                            continue;
                        }

                        float pressure = simulation.GetCellPressure(coord);
                        float oxygen = simulation.DebugGetMoles(coord, AtmosConstants.Oxygen);
                        float carbonDioxide = simulation.DebugGetMoles(coord, AtmosConstants.CarbonDioxide);
                        float totalMoles = 0f;
                        for (int gasId = 0; gasId < AtmosConstants.DefaultGasCount; gasId++)
                        {
                            totalMoles += simulation.DebugGetMoles(coord, new GasId((ushort)gasId));
                        }

                        if (totalMoles <= 0f)
                        {
                            continue;
                        }

                        cellCount++;
                        pressureSum += pressure;
                        oxygenSum += oxygen;
                        carbonDioxideSum += carbonDioxide;
                        totalMolesSum += totalMoles;
                    }
                }
            }

            if (cellCount == 0 || totalMolesSum <= 0f)
            {
                return false;
            }

            sample = new AtmosAreaSample(
                cellCount,
                pressureSum / cellCount,
                oxygenSum / totalMolesSum,
                carbonDioxideSum / totalMolesSum);
            return true;
        }
    }
}
