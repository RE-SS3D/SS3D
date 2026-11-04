using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Builds GPU atlas textures from the authoritative turf gas simulation.
    /// </summary>
    public sealed class AtmosGpuUploader : IDisposable
    {
        // Sim burn intensity is cleared every tick; decay the uploaded fire texture slower so
        // flames read longer than a single 0.2s plasma reaction step.
        private const float VisualFireDecayPerTick = AtmosVisualMetrics.VisualFireDecayPerTick;

        private Texture2D _pressure;
        private Texture2D _temperature;
        private Texture2D _composition;
        private Texture2D _flow;
        private Texture2D _fireIntensity;
        private Texture2D _mask;

        private float[] _pressureScratch;
        private float[] _temperatureScratch;
        private Color32[] _compositionScratch;
        private float[] _flowScratch;
        private float[] _fireScratch;
        private float[] _visualFireScratch;
        private byte[] _maskScratch;

        private int _atlasWidth;
        private int _atlasHeight;
        private int _minTileX;
        private int _minTileZ;
        private int _mapId = -1;
        private bool _valid;

        // Cached method-group delegates so Refresh does not allocate closures each tick.
        private readonly Action<TileCoord, int> _fillCellTexel;
        private AtmosSimulation _activeSimulation;

        public AtmosGpuUploader()
        {
            _fillCellTexel = FillCellTexel;
        }

        public bool IsValid => _valid;

        /// <summary>
        /// Rebuild atlas from simulation. When <paramref name="aoiMinX"/> is set, only that inclusive
        /// tile window is filled (host play AOI); otherwise bounds cover all sim chunks.
        /// </summary>
        public void Refresh(
            AtmosSimulation simulation,
            int? aoiMinX = null,
            int? aoiMinZ = null,
            int? aoiMaxX = null,
            int? aoiMaxZ = null)
        {
            _valid = false;
            if (simulation == null || simulation.CellCount == 0)
                return;

            int minX;
            int minZ;
            int maxX;
            int maxZ;
            if (aoiMinX.HasValue && aoiMinZ.HasValue && aoiMaxX.HasValue && aoiMaxZ.HasValue)
            {
                minX = aoiMinX.Value;
                minZ = aoiMinZ.Value;
                maxX = aoiMaxX.Value;
                maxZ = aoiMaxZ.Value;
                if (maxX < minX || maxZ < minZ)
                    return;
            }
            else if (!TryComputeBounds(simulation, out minX, out minZ, out maxX, out maxZ))
            {
                return;
            }

            int dataWidth = maxX - minX + 1;
            int dataHeight = maxZ - minZ + 1;
            int atlasWidth = NextPowerOfTwo(dataWidth);
            int atlasHeight = NextPowerOfTwo(dataHeight);

            EnsureAtlas(atlasWidth, atlasHeight);
            ClearScratch(atlasWidth * atlasHeight);

            _minTileX = minX;
            _minTileZ = minZ;
            _atlasWidth = atlasWidth;
            _atlasHeight = atlasHeight;
            _mapId = simulation.MapId;
            _activeSimulation = simulation;

            if (aoiMinX.HasValue)
                FillWindow(simulation, minX, minZ, maxX, maxZ);
            else
                simulation.ForEachCell(_fillCellTexel);

            _activeSimulation = null;
            UploadTextures();
            _valid = true;
        }

        private void FillWindow(AtmosSimulation simulation, int minX, int minZ, int maxX, int maxZ)
        {
            int mapId = simulation.MapId;
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    TileCoord coord = new TileCoord(mapId, new Vector2Int(x, z));
                    if (!simulation.TryGetCellIndex(coord, out int cellIndex))
                        continue;

                    _fillCellTexel(coord, cellIndex);
                }
            }
        }

        public AtmosRenderContext.Snapshot BuildSnapshot(GasVisualProfileBuilder.GpuSet gasProfiles)
        {
            return new AtmosRenderContext.Snapshot
            {
                Pressure = _pressure,
                Temperature = _temperature,
                Composition = _composition,
                Flow = _flow,
                FlowData = _flowScratch,
                FireIntensity = _fireIntensity,
                Mask = _mask,
                AtlasBounds = new Vector4(_minTileX, _minTileZ, _atlasWidth, _atlasHeight),
                MapId = _mapId,
                Valid = _valid,
                IgnitionTemperature = AtmosFluxConstants.PlasmaIgnitionTemperature,
                GasScatter = gasProfiles.Scatter,
                GasEmission = gasProfiles.Emission,
                GasMisc = gasProfiles.Misc,
            };
        }

        public void Dispose()
        {
            DestroyTexture(ref _pressure);
            DestroyTexture(ref _temperature);
            DestroyTexture(ref _composition);
            DestroyTexture(ref _flow);
            DestroyTexture(ref _fireIntensity);
            DestroyTexture(ref _mask);
            _activeSimulation = null;
            _valid = false;
        }

        private static bool TryComputeBounds(AtmosSimulation simulation, out int minX, out int minZ, out int maxX, out int maxZ)
        {
            minX = int.MaxValue;
            minZ = int.MaxValue;
            maxX = int.MinValue;
            maxZ = int.MinValue;

            IReadOnlyList<TileChunkRef> chunks = simulation.Chunks;
            if (chunks == null || chunks.Count == 0)
                return false;

            for (int i = 0; i < chunks.Count; i++)
            {
                Vector2Int chunkKey = chunks[i].ChunkKey;
                int chunkMinX = chunkKey.x * AtmosConstants.ChunkSize;
                int chunkMinZ = chunkKey.y * AtmosConstants.ChunkSize;
                int chunkMaxX = chunkMinX + AtmosConstants.ChunkSize - 1;
                int chunkMaxZ = chunkMinZ + AtmosConstants.ChunkSize - 1;

                if (chunkMinX < minX) minX = chunkMinX;
                if (chunkMinZ < minZ) minZ = chunkMinZ;
                if (chunkMaxX > maxX) maxX = chunkMaxX;
                if (chunkMaxZ > maxZ) maxZ = chunkMaxZ;
            }

            return true;
        }

        private void FillCellTexel(TileCoord coord, int cellIndex)
        {
            AtmosSimulation simulation = _activeSimulation;
            int texel = GetTexelIndex(coord.Grid.x - _minTileX, coord.Grid.y - _minTileZ);
            if (texel < 0)
                return;

            AtmosCellMeta meta = simulation.CellMeta[cellIndex];
            _pressureScratch[texel] = simulation.GetCellPressure(cellIndex);
            _temperatureScratch[texel] = meta.Temperature;
            float simFire = simulation.BurnIntensity.IsCreated
                ? simulation.BurnIntensity[cellIndex]
                : 0f;
            float decayedFire = _visualFireScratch[texel] * VisualFireDecayPerTick;
            float visualFire = Mathf.Max(simFire, decayedFire);
            _visualFireScratch[texel] = visualFire;
            _fireScratch[texel] = visualFire;
            _maskScratch[texel] = AtmosCellVisualEncoding.EncodeMask(meta.State);
            _compositionScratch[texel] = AtmosCellVisualEncoding.EncodeComposition(simulation.MolesRead, cellIndex);
            AtmosCellVisualEncoding.PackFlow(
                AtmosCellVisualEncoding.EncodeFlow(AtmosWind.SampleCell(simulation, cellIndex)), _flowScratch, texel);
        }

        private int GetTexelIndex(int localX, int localZ)
        {
            if (localX < 0 || localZ < 0 || localX >= _atlasWidth || localZ >= _atlasHeight)
                return -1;

            return localZ * _atlasWidth + localX;
        }

        private void EnsureAtlas(int width, int height)
        {
            int pixelCount = width * height;
            AtmosGpuAtlasTextureUtility.EnsureTexture(ref _pressure, width, height, TextureFormat.RFloat, FilterMode.Bilinear);
            AtmosGpuAtlasTextureUtility.EnsureTexture(ref _temperature, width, height, TextureFormat.RFloat, FilterMode.Bilinear);
            AtmosGpuAtlasTextureUtility.EnsureTexture(ref _composition, width, height, TextureFormat.RGBA32, FilterMode.Bilinear);
            AtmosGpuAtlasTextureUtility.EnsureTexture(ref _flow, width, height, TextureFormat.RGFloat, FilterMode.Bilinear);
            AtmosGpuAtlasTextureUtility.EnsureTexture(ref _fireIntensity, width, height, TextureFormat.RFloat, FilterMode.Bilinear);
            AtmosGpuAtlasTextureUtility.EnsureTexture(ref _mask, width, height, TextureFormat.R8, FilterMode.Point);

            EnsureScratch(ref _pressureScratch, pixelCount);
            EnsureScratch(ref _temperatureScratch, pixelCount);
            EnsureScratch(ref _compositionScratch, pixelCount);
            EnsureScratch(ref _flowScratch, pixelCount * 2);
            EnsureScratch(ref _fireScratch, pixelCount);
            EnsureScratch(ref _visualFireScratch, pixelCount);
            EnsureScratch(ref _maskScratch, pixelCount);
        }

        private static void EnsureScratch<T>(ref T[] scratch, int length)
        {
            if (scratch == null || scratch.Length != length)
                scratch = new T[length];
        }

        private void ClearScratch(int pixelCount)
        {
            Array.Clear(_pressureScratch, 0, pixelCount);
            Array.Clear(_temperatureScratch, 0, pixelCount);
            Array.Clear(_fireScratch, 0, pixelCount);
            Array.Clear(_maskScratch, 0, pixelCount);

            var clearComposition = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixelCount; i++)
                _compositionScratch[i] = clearComposition;

            for (int i = 0; i < pixelCount * 2; i += 2)
            {
                _flowScratch[i] = 0.5f;
                _flowScratch[i + 1] = 0.5f;
            }
        }

        private void UploadTextures()
        {
            _pressure.SetPixelData(_pressureScratch, 0);
            _pressure.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            _temperature.SetPixelData(_temperatureScratch, 0);
            _temperature.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            _composition.SetPixelData(_compositionScratch, 0);
            _composition.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            _flow.SetPixelData(_flowScratch, 0);
            _flow.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            _fireIntensity.SetPixelData(_fireScratch, 0);
            _fireIntensity.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            _mask.SetPixelData(_maskScratch, 0);
            _mask.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        }

        private static int NextPowerOfTwo(int value) => AtmosGpuAtlasTextureUtility.NextPowerOfTwo(value);

        private static void DestroyTexture(ref Texture2D texture) => AtmosGpuAtlasTextureUtility.DestroyTexture(ref texture);
    }
}
