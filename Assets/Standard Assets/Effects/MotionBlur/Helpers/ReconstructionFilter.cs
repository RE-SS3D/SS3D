using UnityEngine;

namespace UnityStandardAssets.CinematicEffects
{
    public partial class MotionBlur
    {
        // Reconstruction filter for shutter speed simulation
        class ReconstructionFilter
        {
            #region Predefined constants

            // The maximum length of motion blur, given as a percentage
            // of the screen height. Larger values may introduce artifacts.
            const float kMaxBlurRadius = 5;

            #endregion

            #region Public methods

            public ReconstructionFilter()
            {
                var shader = Shader.Find("Hidden/Image Effects/Cinematic/MotionBlur/Reconstruction");
                if (shader.isSupported && CheckTextureFormatSupport()) {
                    _material = new Material(shader);
                    _material.hideFlags = HideFlags.DontSave;
                }

                // Use loop unrolling on Adreno GPUs to avoid shader issues.
                _unroll = SystemInfo.graphicsDeviceName.Contains("Adreno");

                FetchUniformLocations();
            }

            public void Release()
            {
                if (_material != null) DestroyImmediate(_material);
                _material = null;
            }

            public void ProcessImage(float shutterAngle, int sampleCount, RenderTexture source, RenderTexture destination)
            {
                // If the shader isn't supported, simply blit and return.
                if (_material == null) {
                    Graphics.Blit(source, destination);
                    return;
                }

                // Calculate the maximum blur radius in pixels.
                var maxBlurPixels = (int)(kMaxBlurRadius * source.height / 100);

                // Calculate the TileMax size.
                // It should be a multiple of 8 and larger than maxBlur.
                var tileSize = ((maxBlurPixels - 1) / 8 + 1) * 8;

                // 1st pass - Velocity/depth packing
                // Motion vectors are scaled by an empirical factor of 1.45.
                var velocityScale = shutterAngle / 360 * 1.45f;
                _material.SetFloat(_VelocityScale, velocityScale);
                _material.SetFloat(_MaxBlurRadius, maxBlurPixels);

                var vbuffer = GetTemporaryRT(source, 1, _packedRTFormat);
                Graphics.Blit(null, vbuffer, _material, 0);

                // 2nd pass - 1/4 TileMax filter
                var tile4 = GetTemporaryRT(source, 4, _vectorRTFormat);
                Graphics.Blit(vbuffer, tile4, _material, 1);

                // 3rd pass - 1/2 TileMax filter
                var tile8 = GetTemporaryRT(source, 8, _vectorRTFormat);
                Graphics.Blit(tile4, tile8, _material, 2);
                ReleaseTemporaryRT(tile4);

                // 4th pass - Last TileMax filter (reduce to tileSize)
                var tileMaxOffs = Vector2.one * (tileSize / 8.0f - 1) * -0.5f;
                _material.SetVector(_TileMaxOffs, tileMaxOffs);
                _material.SetInt(_TileMaxLoop, tileSize / 8);

                var tile = GetTemporaryRT(source, tileSize, _vectorRTFormat);
                Graphics.Blit(tile8, tile, _material, 3);
                ReleaseTemporaryRT(tile8);

                // 5th pass - NeighborMax filter
                var neighborMax = GetTemporaryRT(source, tileSize, _vectorRTFormat);
                Graphics.Blit(tile, neighborMax, _material, 4);
                ReleaseTemporaryRT(tile);

                // 6th pass - Reconstruction pass
                _material.SetInt(_LoopCount, Mathf.Clamp(sampleCount / 2, 1, 64));
                _material.SetFloat(_MaxBlurRadius, maxBlurPixels);
                _material.SetTexture(_NeighborMaxTex, neighborMax);
                _material.SetTexture(_VelocityTex, vbuffer);
                Graphics.Blit(source, destination, _material, _unroll ? 6 : 5);

                // Cleaning up
                ReleaseTemporaryRT(vbuffer);
                ReleaseTemporaryRT(neighborMax);
            }

            #endregion

            #region Private members

            Material _material;
            bool _unroll;

            // Texture format for storing 2D vectors.
            RenderTextureFormat _vectorRTFormat = RenderTextureFormat.RGHalf;

            // Texture format for storing packed velocity/depth.
            RenderTextureFormat _packedRTFormat = RenderTextureFormat.ARGB2101010;

            int _VelocityScale;
            int _MaxBlurRadius;
            int _TileMaxOffs;
            int _TileMaxLoop;
            int _LoopCount;
            int _NeighborMaxTex;
            int _VelocityTex;

            bool CheckTextureFormatSupport()
            {
                // RGHalf is not supported = Can't use motion vectors.
                if (!SystemInfo.SupportsRenderTextureFormat(_vectorRTFormat))
                    return false;

                // If 2:10:10:10 isn't supported, use ARGB32 instead.
                if (!SystemInfo.SupportsRenderTextureFormat(_packedRTFormat))
                    _packedRTFormat = RenderTextureFormat.ARGB32;

                return true;
            }

            RenderTexture GetTemporaryRT(Texture source, int divider, RenderTextureFormat format)
            {
                var w = source.width / divider;
                var h = source.height / divider;
                var rt = RenderTexture.GetTemporary(w, h, 0, format);
                rt.filterMode = FilterMode.Point;
                return rt;
            }

            void ReleaseTemporaryRT(RenderTexture rt)
            {
                RenderTexture.ReleaseTemporary(rt);
            }

            void FetchUniformLocations()
            {
                _VelocityScale = Shader.PropertyToID("_VelocityScale");
                _MaxBlurRadius = Shader.PropertyToID("_MaxBlurRadius");
                _TileMaxOffs = Shader.PropertyToID("_TileMaxOffs");
                _TileMaxLoop = Shader.PropertyToID("_TileMaxLoop");
                _LoopCount = Shader.PropertyToID("_LoopCount");
                _NeighborMaxTex = Shader.PropertyToID("_NeighborMaxTex");
                _VelocityTex = Shader.PropertyToID("_VelocityTex");
            }

            #endregion
        }
    }
}
