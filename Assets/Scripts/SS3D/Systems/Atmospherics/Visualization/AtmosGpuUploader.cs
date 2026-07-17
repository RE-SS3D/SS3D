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
        private const byte MaskEmpty = 0;
        private const byte MaskSimulated = 1;
        private const byte MaskVacuum = 2;
        private const byte MaskBlocked = 3;
        // Sim burn intensity is cleared every tick; decay the uploaded fire texture slower so
        // flames read longer than a single 0.2s plasma reaction step.
        private const float VisualFireDecayPerTick = 0.96f;

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
        private readonly Action<TileCoord, int> _writeFlowTexel;
        private AtmosSimulation _activeSimulation;

        public AtmosGpuUploader()
        {
            _fillCellTexel = FillCellTexel;
            _writeFlowTexel = WriteFlowTexel;
        }

        public bool IsValid => _valid;

        public void Refresh(AtmosSimulation simulation)
        {
            _valid = false;
            if (simulation == null || simulation.CellCount == 0)
                return;

            if (!TryComputeBounds(simulation, out int minX, out int minZ, out int maxX, out int maxZ))
                return;

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

            simulation.ForEachCell(_fillCellTexel);
            simulation.ForEachCell(_writeFlowTexel);

            _activeSimulation = null;
            UploadTextures();
            _valid = true;
        }

        public AtmosRenderContext.Snapshot BuildSnapshot(GasVisualProfileBuilder.GpuSet gasProfiles)
        {
            return new AtmosRenderContext.Snapshot
            {
                Pressure = _pressure,
                Temperature = _temperature,
                Composition = _composition,
                Flow = _flow,
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
            _maskScratch[texel] = EncodeMask(meta.State);
            _compositionScratch[texel] = EncodeComposition(simulation, cellIndex);
        }

        private void WriteFlowTexel(TileCoord coord, int _)
        {
            int texel = GetTexelIndex(coord.Grid.x - _minTileX, coord.Grid.y - _minTileZ);
            if (texel < 0 || _maskScratch[texel] == MaskEmpty)
                return;

            float pressure = _pressureScratch[texel];
            float gradientX = SamplePressureOffset(coord.Grid.x, coord.Grid.y, 1, 0) - pressure;
            float gradientZ = SamplePressureOffset(coord.Grid.x, coord.Grid.y, 0, 1) - pressure;

            Vector2 gradient = new Vector2(gradientX, gradientZ);
            if (gradient.sqrMagnitude > 1e-6f)
                gradient = gradient.normalized;

            // Pack -1..1 into 0..1 for RG storage; shader unpacks in Phase 2.
            int flowIndex = texel * 2;
            _flowScratch[flowIndex] = gradient.x * 0.5f + 0.5f;
            _flowScratch[flowIndex + 1] = gradient.y * 0.5f + 0.5f;
        }

        private float SamplePressureOffset(int tileX, int tileZ, int offsetX, int offsetZ)
        {
            int texel = GetTexelIndex(tileX + offsetX - _minTileX, tileZ + offsetZ - _minTileZ);
            if (texel < 0 || _maskScratch[texel] == MaskEmpty)
                return 0f;

            return _pressureScratch[texel];
        }

        private static Color32 EncodeComposition(AtmosSimulation simulation, int cellIndex)
        {
            float m0 = simulation.MolesRead[GasMixture.GetMoleIndex(cellIndex, AtmosConstants.Oxygen)];
            float m1 = simulation.MolesRead[GasMixture.GetMoleIndex(cellIndex, AtmosConstants.Nitrogen)];
            float m2 = simulation.MolesRead[GasMixture.GetMoleIndex(cellIndex, AtmosConstants.CarbonDioxide)];
            float m3 = simulation.MolesRead[GasMixture.GetMoleIndex(cellIndex, AtmosConstants.Plasma)];
            float totalMoles = m0 + m1 + m2 + m3;

            if (totalMoles <= 1e-6f)
                return new Color32(0, 0, 0, 0);

            return new Color32(
                ToByte(m0 / totalMoles),
                ToByte(m1 / totalMoles),
                ToByte(m2 / totalMoles),
                ToByte(m3 / totalMoles));
        }

        private static byte EncodeMask(AtmosCellState state)
        {
            return state switch
            {
                AtmosCellState.Vacuum => MaskVacuum,
                AtmosCellState.Blocked => MaskBlocked,
                AtmosCellState.Active or AtmosCellState.Semiactive or AtmosCellState.Inactive => MaskSimulated,
                _ => MaskEmpty,
            };
        }

        private static byte ToByte(float normalized)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(normalized * 255f), 0, 255);
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
            EnsureTexture(ref _pressure, width, height, TextureFormat.RFloat, FilterMode.Bilinear);
            EnsureTexture(ref _temperature, width, height, TextureFormat.RFloat, FilterMode.Bilinear);
            EnsureTexture(ref _composition, width, height, TextureFormat.RGBA32, FilterMode.Bilinear);
            EnsureTexture(ref _flow, width, height, TextureFormat.RGFloat, FilterMode.Bilinear);
            EnsureTexture(ref _fireIntensity, width, height, TextureFormat.RFloat, FilterMode.Bilinear);
            EnsureTexture(ref _mask, width, height, TextureFormat.R8, FilterMode.Point);

            EnsureScratch(ref _pressureScratch, pixelCount);
            EnsureScratch(ref _temperatureScratch, pixelCount);
            EnsureScratch(ref _compositionScratch, pixelCount);
            EnsureScratch(ref _flowScratch, pixelCount * 2);
            EnsureScratch(ref _fireScratch, pixelCount);
            EnsureScratch(ref _visualFireScratch, pixelCount);
            EnsureScratch(ref _maskScratch, pixelCount);
        }

        private static void EnsureTexture(
            ref Texture2D texture,
            int width,
            int height,
            TextureFormat format,
            FilterMode filterMode)
        {
            if (texture != null && texture.width == width && texture.height == height && texture.format == format)
            {
                texture.filterMode = filterMode;
                texture.wrapMode = TextureWrapMode.Clamp;
                return;
            }

            if (texture != null)
                DestroyObject(texture);

            texture = new Texture2D(width, height, format, mipChain: false, linear: true)
            {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Atmos{format}",
            };
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

        private static int NextPowerOfTwo(int value)
        {
            int power = 1;
            while (power < value)
                power <<= 1;
            return power;
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
                return;

            DestroyObject(texture);
            texture = null;
        }

        private static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
