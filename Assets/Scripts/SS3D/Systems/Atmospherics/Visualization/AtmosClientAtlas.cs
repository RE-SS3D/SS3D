using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics.ECS;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Client-side counterpart to <see cref="AtmosGpuUploader"/>. Instead of reading a local
    /// simulation, it caches the latest <see cref="AtmosChunkPatch"/> per chunk received over the
    /// network and rebuilds the same GPU atlas layout from that cache.
    /// </summary>
    public sealed class AtmosClientAtlas : IDisposable
    {
        private readonly Dictionary<Vector2Int, AtmosChunkPatch> _chunks = new();

        private Texture2D _pressure;
        private Texture2D _temperature;
        private Texture2D _composition;
        private Texture2D _flow;
        private Texture2D _fireIntensity;
        private Texture2D _mask;

        private float[] _pressureScratch = Array.Empty<float>();
        private float[] _temperatureScratch = Array.Empty<float>();
        private Color32[] _compositionScratch = Array.Empty<Color32>();
        private float[] _flowScratch = Array.Empty<float>();
        private float[] _fireScratch = Array.Empty<float>();
        private byte[] _maskScratch = Array.Empty<byte>();

        private int _atlasWidth;
        private int _atlasHeight;
        private int _minTileX;
        private int _minTileZ;
        private int _mapId = -1;
        private bool _valid;

        public bool IsValid => _valid;

        public void ApplyChunkPatch(AtmosChunkPatch patch)
        {
            _chunks[patch.ChunkKey] = patch;
            _mapId = patch.MapId;

            ComputeBounds(out int minX, out int minZ, out int atlasWidth, out int atlasHeight);
            bool boundsChanged = !_valid
                || minX != _minTileX || minZ != _minTileZ
                || atlasWidth != _atlasWidth || atlasHeight != _atlasHeight;

            if (boundsChanged)
            {
                _minTileX = minX;
                _minTileZ = minZ;
                _atlasWidth = atlasWidth;
                _atlasHeight = atlasHeight;

                EnsureAtlas(atlasWidth, atlasHeight);
                ClearScratch(atlasWidth * atlasHeight);

                foreach (AtmosChunkPatch cached in _chunks.Values)
                    WriteChunkIntoScratch(cached);
            }
            else
            {
                WriteChunkIntoScratch(patch);
            }

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
            AtmosGpuAtlasTextureUtility.DestroyTexture(ref _pressure);
            AtmosGpuAtlasTextureUtility.DestroyTexture(ref _temperature);
            AtmosGpuAtlasTextureUtility.DestroyTexture(ref _composition);
            AtmosGpuAtlasTextureUtility.DestroyTexture(ref _flow);
            AtmosGpuAtlasTextureUtility.DestroyTexture(ref _fireIntensity);
            AtmosGpuAtlasTextureUtility.DestroyTexture(ref _mask);
            _chunks.Clear();
            _valid = false;
        }

        private void ComputeBounds(out int minX, out int minZ, out int atlasWidth, out int atlasHeight)
        {
            int localMinX = int.MaxValue;
            int localMinZ = int.MaxValue;
            int localMaxX = int.MinValue;
            int localMaxZ = int.MinValue;

            foreach (Vector2Int key in _chunks.Keys)
            {
                int originX = key.x * AtmosConstants.ChunkSize;
                int originZ = key.y * AtmosConstants.ChunkSize;
                localMinX = Mathf.Min(localMinX, originX);
                localMinZ = Mathf.Min(localMinZ, originZ);
                localMaxX = Mathf.Max(localMaxX, originX + AtmosConstants.ChunkSize - 1);
                localMaxZ = Mathf.Max(localMaxZ, originZ + AtmosConstants.ChunkSize - 1);
            }

            minX = localMinX;
            minZ = localMinZ;
            int dataWidth = localMaxX - localMinX + 1;
            int dataHeight = localMaxZ - localMinZ + 1;
            atlasWidth = AtmosGpuAtlasTextureUtility.NextPowerOfTwo(dataWidth);
            atlasHeight = AtmosGpuAtlasTextureUtility.NextPowerOfTwo(dataHeight);
        }

        private void WriteChunkIntoScratch(AtmosChunkPatch patch)
        {
            int chunkOriginX = patch.ChunkKey.x * AtmosConstants.ChunkSize;
            int chunkOriginZ = patch.ChunkKey.y * AtmosConstants.ChunkSize;

            for (int localZ = 0; localZ < AtmosConstants.ChunkSize; localZ++)
            {
                for (int localX = 0; localX < AtmosConstants.ChunkSize; localX++)
                {
                    int local = localZ * AtmosConstants.ChunkSize + localX;
                    int texel = GetTexelIndex(chunkOriginX + localX - _minTileX, chunkOriginZ + localZ - _minTileZ);
                    if (texel < 0)
                        continue;

                    _pressureScratch[texel] = patch.Pressure[local];
                    _temperatureScratch[texel] = patch.Temperature[local];
                    _compositionScratch[texel] = patch.Composition[local];
                    _fireScratch[texel] = patch.FireIntensity[local];
                    _maskScratch[texel] = patch.Mask[local];

                    int flowIndex = texel * 2;
                    int patchFlowIndex = local * 2;
                    _flowScratch[flowIndex] = patch.Flow[patchFlowIndex];
                    _flowScratch[flowIndex + 1] = patch.Flow[patchFlowIndex + 1];
                }
            }
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
    }
}
