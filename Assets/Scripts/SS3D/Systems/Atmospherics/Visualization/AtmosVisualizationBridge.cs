using SS3D.Core.Behaviours;
using SS3D.Rendering.URP;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Uploads authoritative atmos sim data to GPU textures after each tick.
    /// </summary>
    public sealed class AtmosVisualizationBridge : Actor
    {
        private AtmosSubSystem _atmos;
        private AtmosGpuUploader _uploader;

        protected override void OnStart()
        {
            _atmos = GetComponent<AtmosSubSystem>();
            _uploader = new AtmosGpuUploader();
        }

        protected override void OnDestroyed()
        {
            AtmosRenderContext.ClearSnapshot();
            _uploader?.Dispose();
            _uploader = null;
        }

        /// <summary>Called by <see cref="AtmosSubSystem"/> after each simulation tick.</summary>
        public void PublishSnapshot()
        {
            // When synthetic debug source is active, keep its snapshot authoritative.
            if (AtmosRenderContext.IsDebugSnapshotOverrideEnabled())
                return;

            if (_uploader == null || _atmos?.Simulation == null)
            {
                AtmosRenderContext.ClearSnapshot();
                return;
            }

            _uploader.Refresh(_atmos.Simulation);
            if (!_uploader.IsValid)
            {
                AtmosRenderContext.ClearSnapshot();
                return;
            }

            AtmosRenderContext.SetSnapshot(_uploader.BuildSnapshot());
        }
    }
}
