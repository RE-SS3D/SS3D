using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace EditorTests.Atmospherics
{
    internal readonly struct ReactAtmosJobResult
    {
        internal readonly float PlasmaBurned;
        internal readonly float OxygenConsumed;
        internal readonly float CarbonDioxideProduced;
        internal readonly float BurnIntensity;
        internal readonly float TotalMolesBefore;
        internal readonly float TotalMolesAfter;

        internal ReactAtmosJobResult(
            float plasmaBurned,
            float oxygenConsumed,
            float carbonDioxideProduced,
            float burnIntensity,
            float totalMolesBefore,
            float totalMolesAfter)
        {
            PlasmaBurned = plasmaBurned;
            OxygenConsumed = oxygenConsumed;
            CarbonDioxideProduced = carbonDioxideProduced;
            BurnIntensity = burnIntensity;
            TotalMolesBefore = totalMolesBefore;
            TotalMolesAfter = totalMolesAfter;
        }
    }

    internal static class AtmosTestFixtures
    {
        internal static AtmosSimulation CreateSealedRoomSimulation(
            TileMapTestUtilities.MapContext context,
            int size,
            out int mapId)
        {
            const int origin = 0;
            TileMapTestUtilities.BuildWalledRoom(context, origin, origin, size);

            mapId = context.Map.MapId;
            var simulation = new AtmosSimulation(context.Query, mapId, AtmosConstants.DefaultGasCount);
            simulation.CreateChunk(new TileChunkRef
            {
                MapId = mapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });

            int outerSize = size + 2;
            for (int x = 0; x < outerSize; x++)
            {
                for (int z = 0; z < outerSize; z++)
                    simulation.UpdateCell(new TileCoord(mapId, origin + x, origin + z));
            }

            return simulation;
        }

        internal static int InteriorOrigin => 1;

        internal static TileCoord InteriorCoord(int mapId, int interiorSize) =>
            new TileCoord(mapId, InteriorOrigin + (interiorSize - 1) / 2, InteriorOrigin + (interiorSize - 1) / 2);

        internal static void IgnitePlasmaFire(AtmosSimulation simulation, TileCoord coord, float plasmaMoles, float oxygenMoles)
        {
            simulation.DebugAddMoles(coord, AtmosConstants.Plasma, plasmaMoles);
            simulation.DebugAddMoles(coord, AtmosConstants.Oxygen, oxygenMoles);
            simulation.DebugSetTemperature(coord, 1000f);
        }

        internal static float SumGas(AtmosSimulation simulation, int mapId, int interiorSize, GasId gasId)
        {
            float total = 0f;
            for (int x = 0; x < interiorSize; x++)
            {
                for (int z = 0; z < interiorSize; z++)
                {
                    total += simulation.DebugGetMoles(
                        new TileCoord(mapId, InteriorOrigin + x, InteriorOrigin + z),
                        gasId);
                }
            }

            return total;
        }

        internal static ReactAtmosJobResult RunReactAtmosJobOnCell(
            float plasmaMoles,
            float oxygenMoles,
            float carbonDioxideMoles,
            float nitrogenMoles,
            float temperature,
            float deltaTime)
        {
            const int maxGasTypes = AtmosConstants.MaxGasTypes;
            const int gasTypeCount = AtmosConstants.DefaultGasCount;

            var moles = new NativeArray<float>(maxGasTypes, Allocator.Temp);
            var meta = new NativeArray<AtmosCellMeta>(1, Allocator.Temp);
            var burn = new NativeArray<float>(1, Allocator.Temp);
            var active = new NativeArray<int>(1, Allocator.Temp);
            var specificHeats = new NativeArray<float>(maxGasTypes, Allocator.Temp);

            try
            {
                foreach (GasDefault gas in GasDefaults.Core)
                    specificHeats[gas.Id] = gas.SpecificHeat;

                int oxygenId = AtmosConstants.Oxygen.Value;
                int nitrogenId = AtmosConstants.Nitrogen.Value;
                int carbonDioxideId = AtmosConstants.CarbonDioxide.Value;
                int plasmaId = AtmosConstants.Plasma.Value;

                moles[oxygenId] = oxygenMoles;
                moles[nitrogenId] = nitrogenMoles;
                moles[carbonDioxideId] = carbonDioxideMoles;
                moles[plasmaId] = plasmaMoles;

                meta[0] = new AtmosCellMeta
                {
                    Temperature = temperature,
                    Volume = AtmosConstants.CellVolume,
                    State = AtmosCellState.Active,
                };
                active[0] = 0;

                float totalBefore = oxygenMoles + nitrogenMoles + carbonDioxideMoles + plasmaMoles;

                var reactJob = new ReactAtmosJob
                {
                    MaxGasTypes = maxGasTypes,
                    GasTypeCount = gasTypeCount,
                    OxygenId = oxygenId,
                    PlasmaId = plasmaId,
                    CarbonDioxideId = carbonDioxideId,
                    DeltaTime = deltaTime,
                    ActiveCells = active,
                    SpecificHeat = specificHeats,
                    Moles = moles,
                    CellMeta = meta,
                    BurnIntensity = burn,
                };
                reactJob.Execute();

                float plasmaAfter = moles[plasmaId];
                float oxygenAfter = moles[oxygenId];
                float carbonDioxideAfter = moles[carbonDioxideId];
                float totalAfter = oxygenAfter + moles[nitrogenId] + carbonDioxideAfter + plasmaAfter;

                return new ReactAtmosJobResult(
                    plasmaMoles - plasmaAfter,
                    oxygenMoles - oxygenAfter,
                    carbonDioxideAfter - carbonDioxideMoles,
                    burn[0],
                    totalBefore,
                    totalAfter);
            }
            finally
            {
                if (moles.IsCreated)
                    moles.Dispose();
                if (meta.IsCreated)
                    meta.Dispose();
                if (burn.IsCreated)
                    burn.Dispose();
                if (active.IsCreated)
                    active.Dispose();
                if (specificHeats.IsCreated)
                    specificHeats.Dispose();
            }
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
