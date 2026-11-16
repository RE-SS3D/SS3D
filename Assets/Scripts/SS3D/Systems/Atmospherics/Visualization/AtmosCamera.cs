using SS3D.Core.Behaviours;
using SS3D.Rendering.URP;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Registers the player camera with <see cref="AtmosRenderContext"/> before each render,
    /// mirroring how <see cref="Selection.SelectionCamera"/> drives selection picking.
    /// Sim data upload stays on <see cref="AtmosVisualizationBridge"/>.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class AtmosCamera : Actor
    {
        private Camera _playerCamera;

        protected override void OnStart()
        {
            _playerCamera = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        protected override void OnDisabled()
        {
            AtmosRenderContext.ClearRequest();
        }

        protected override void OnDestroyed()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            AtmosRenderContext.ClearRequest();
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _playerCamera)
                return;

            AtmosRenderContext.SetRequest(new AtmosRenderContext.Request
            {
                SourceCamera = _playerCamera,
            });
        }
    }
}
