using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using System;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Builds GPU atlas textures from the authoritative turf gas simulation.
    /// </summary>
    public sealed class AtmosGpuUploader : IDisposable
    {
        private const int CompositionGasChannels = 4;
        private const byte MaskEmpty = 0;
        private const byte MaskSimulated = 1;
        private const byte MaskVacuum = 2;
        private const byte MaskBlocked = 3;

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
        private byte[] _maskScratch;

        private int _atlasWidth;
        private int _atlasHeight;
        private int _minTileX;
        private int _minTileZ;
        private int _mapId = -1;
        private bool _valid;

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

            simulation.ForEachCell((coord, cellIndex) =>
            {
                int texel = GetTexelIndex(coord.Grid.x - minX, coord.Grid.y - minZ);
                if (texel < 0)
                    return;

                AtmosCellMeta meta = simulation.CellMeta[cellIndex];
                _pressureScratch[texel] = simulation.GetCellPressure(cellIndex);
                _temperatureScratch[texel] = meta.Temperature;
                _fireScratch[texel] = simulation.BurnIntensity.IsCreated
                    ? simulation.BurnIntensity[cellIndex]
                    : 0f;
                _maskScratch[texel] = EncodeMask(meta.State);
                _compositionScratch[texel] = EncodeComposition(simulation, cellIndex);
            });

            WriteFlowGradients(simulation, minX, minZ);
            UploadTextures();
            _valid = true;
        }

        public AtmosRenderContext.Snapshot BuildSnapshot()
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
            _valid = false;
        }

        private static bool TryComputeBounds(AtmosSimulation simulation, out int minX, out int minZ, out int maxX, out int maxZ)
        {
            int localMinX = int.MaxValue;
            int localMinZ = int.MaxValue;
            int localMaxX = int.MinValue;
            int localMaxZ = int.MinValue;

            simulation.ForEachCoord(coord =>
            {
                localMinX = Mathf.Min(localMinX, coord.Grid.x);
                localMinZ = Mathf.Min(localMinZ, coord.Grid.y);
                localMaxX = Mathf.Max(localMaxX, coord.Grid.x);
                localMaxZ = Mathf.Max(localMaxZ, coord.Grid.y);
            });

            minX = localMinX;
            minZ = localMinZ;
            maxX = localMaxX;
            maxZ = localMaxZ;
            return localMinX != int.MaxValue;
        }

        private void WriteFlowGradients(AtmosSimulation simulation, int minX, int minZ)
        {
            simulation.ForEachCell((coord, cellIndex) =>
            {
                int texel = GetTexelIndex(coord.Grid.x - minX, coord.Grid.y - minZ);
                if (texel < 0 || _maskScratch[texel] == MaskEmpty)
                    return;

                float pressure = _pressureScratch[texel];
                float gradientX = SamplePressureOffset(simulation, coord, 1, 0, minX, minZ) - pressure;
                float gradientZ = SamplePressureOffset(simulation, coord, 0, 1, minX, minZ) - pressure;

                Vector2 gradient = new Vector2(gradientX, gradientZ);
                if (gradient.sqrMagnitude > 1e-6f)
                    gradient = gradient.normalized;

                // Pack -1..1 into 0..1 for RG storage; shader unpacks in Phase 2.
                int flowIndex = texel * 2;
                _flowScratch[flowIndex] = gradient.x * 0.5f + 0.5f;
                _flowScratch[flowIndex + 1] = gradient.y * 0.5f + 0.5f;
            });
        }

        private float SamplePressureOffset(
            AtmosSimulation simulation,
            TileCoord coord,
            int offsetX,
            int offsetZ,
            int minX,
            int minZ)
        {
            var neighbourCoord = new TileCoord(coord.MapId, coord.Grid.x + offsetX, coord.Grid.y + offsetZ);
            if (!simulation.TryGetCellIndex(neighbourCoord, out int neighbourIndex))
                return 0f;

            int texel = GetTexelIndex(neighbourCoord.Grid.x - minX, neighbourCoord.Grid.y - minZ);
            if (texel < 0)
                return simulation.GetCellPressure(neighbourIndex);

            return _pressureScratch[texel];
        }

        private Color32 EncodeComposition(AtmosSimulation simulation, int cellIndex)
        {
            float totalMoles = 0f;
            var moles = new float[CompositionGasChannels];

            for (int gasId = 0; gasId < CompositionGasChannels; gasId++)
            {
                moles[gasId] = simulation.MolesRead[GasMixture.GetMoleIndex(cellIndex, new GasId((ushort)gasId))];
                totalMoles += moles[gasId];
            }

            if (totalMoles <= 1e-6f)
                return new Color32(0, 0, 0, 0);

            return new Color32(
                ToByte(moles[0] / totalMoles),
                ToByte(moles[1] / totalMoles),
                ToByte(moles[2] / totalMoles),
                ToByte(moles[3] / totalMoles));
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
                UnityEngine.Object.Destroy(texture);

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

            UnityEngine.Object.Destroy(texture);
            texture = null;
        }
    }
}
