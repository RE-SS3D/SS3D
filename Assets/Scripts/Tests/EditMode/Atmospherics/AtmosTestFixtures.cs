using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace EditorTests.Atmospherics
{
    internal static class AtmosTestFixtures
    {
        internal static AtmosSimulation CreateSealedRoomSimulation(
            TileMapTestUtilities.MapContext context,
            int size,
            out int mapId)
        {
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                    TileMapTestUtilities.PlacePlenum(context, new Vector3(x, 0, z));
            }

            mapId = context.Map.MapId;
            var simulation = new AtmosSimulation(context.Query, mapId, AtmosConstants.DefaultGasCount);
            simulation.CreateChunk(new TileChunkRef
            {
                MapId = mapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                    simulation.UpdateCell(new TileCoord(mapId, x, z));
            }

            return simulation;
        }

        internal static void IgnitePlasmaFire(AtmosSimulation simulation, TileCoord coord, float plasmaMoles, float oxygenMoles)
        {
            simulation.DebugAddMoles(coord, AtmosConstants.Plasma, plasmaMoles);
            simulation.DebugAddMoles(coord, AtmosConstants.Oxygen, oxygenMoles);
            simulation.DebugSetTemperature(coord, 1000f);
        }

        internal static float SumGas(AtmosSimulation simulation, int mapId, int size, GasId gasId)
        {
            float total = 0f;
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                    total += simulation.DebugGetMoles(new TileCoord(mapId, x, z), gasId);
            }

            return total;
        }

        internal static float SampleSnapshotScalar(
            AtmosRenderContext.Snapshot snapshot,
            TileCoord coord,
            Texture2D texture)
        {
            int localX = coord.Grid.x - (int)snapshot.AtlasBounds.x;
            int localZ = coord.Grid.y - (int)snapshot.AtlasBounds.y;
            int atlasWidth = (int)snapshot.AtlasBounds.z;
            int index = localZ * atlasWidth + localX;
            return texture.GetPixelData<float>(0)[index];
        }
    }
}
