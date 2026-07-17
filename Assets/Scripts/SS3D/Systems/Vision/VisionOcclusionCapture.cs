using Coimbra;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// Captures a real GPU depth cubemap from the player's position each frame and exposes it as
    /// the authoritative "distance to nearest occluder" source for FOV, instead of the CPU tile
    /// grid VisionGridCaster used to walk. A per-tile boolean grid has no notion of real wall
    /// shape (thin walls, corners, doors), so it let hidden geometry (a wall behind another wall)
    /// leak through. This mirrors the fix already applied to atmos's wall-occlusion bleed - see
    /// AtmosCommon.hlsl's AtmosIsSampleOccluded - trust the real rendered depth buffer, not a
    /// flattened world-XZ abstraction.
    /// </summary>
    internal sealed class VisionOcclusionCapture
    {
        private const int Resolution = 128;

        // Vision queries are always horizontal (direction.y == 0), so the top/bottom cube faces
        // are never sampled and are skipped to save the render cost.
        private const int SideFacesMask =
            (1 << (int)CubemapFace.PositiveX) | (1 << (int)CubemapFace.NegativeX) |
            (1 << (int)CubemapFace.PositiveZ) | (1 << (int)CubemapFace.NegativeZ);

        private static readonly int s_DepthTexId = Shader.PropertyToID("_VisionOcclusionDepth");
        private static readonly int s_ZParamsId = Shader.PropertyToID("_VisionOcclusionZParams");

        private Camera _camera;
        private RenderTexture _depthCubemap;

        public void Capture(Vector3 position, float viewRange, LayerMask occluderMask)
        {
            EnsureResources();

            _camera.transform.position = position;
            _camera.nearClipPlane = 0.05f;
            _camera.farClipPlane = Mathf.Max(viewRange, _camera.nearClipPlane + 0.1f);
            _camera.cullingMask = occluderMask;

            _camera.RenderToCubemap(_depthCubemap, SideFacesMask);

            Shader.SetGlobalTexture(s_DepthTexId, _depthCubemap);
            Shader.SetGlobalVector(s_ZParamsId, ComputeZBufferParams(_camera.nearClipPlane, _camera.farClipPlane));
        }

        public void Dispose()
        {
            if (_camera != null)
                _camera.gameObject.Dispose(true);

            if (_depthCubemap != null)
                _depthCubemap.Release();

            _camera = null;
            _depthCubemap = null;
        }

        private void EnsureResources()
        {
            if (_camera != null)
                return;

            GameObject host = new("VisionOcclusionCaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };

            _camera = host.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.Depth;
            _camera.fieldOfView = 90f;
            _camera.aspect = 1f;
            _camera.allowMSAA = false;
            _camera.allowHDR = false;
            _camera.useOcclusionCulling = false;

            _depthCubemap = new RenderTexture(Resolution, Resolution, 24, RenderTextureFormat.Depth)
            {
                dimension = TextureDimension.Cube,
                name = "VisionOcclusionDepthCube",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _depthCubemap.Create();
        }

        // Matches Unity's own _ZBufferParams convention (see "Built-in shader variables"), computed
        // by hand for this capture camera since it isn't the camera URP binds _ZBufferParams for.
        private static Vector4 ComputeZBufferParams(float near, float far)
        {
            float ratio = far / near;

            return SystemInfo.usesReversedZBuffer
                ? new Vector4(ratio - 1f, 1f, (ratio - 1f) / far, 1f / far)
                : new Vector4(1f - ratio, ratio, (1f - ratio) / far, ratio / far);
        }
    }
}
