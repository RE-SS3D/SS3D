using SS3D.Core.Behaviours;
using SS3D.Rendering.URP;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Pure-client counterpart to <see cref="AtmosVisualizationBridge"/>. Has no
    /// <see cref="AtmosSimulation"/> of its own; instead it builds its atlas from
    /// <see cref="AtmosChunkPatch"/> data pushed by the server (see
    /// <see cref="AtmosSubSystem"/>'s dirty-chunk broadcast).
    /// </summary>
    public sealed class AtmosClientVisualizationBridge : Actor
    {
        private AtmosSubSystem _atmos;
        private AtmosClientAtlas _atlas;

        protected override void OnStart()
        {
            _atmos = GetComponent<AtmosSubSystem>();
            _atlas = new AtmosClientAtlas();
        }

        protected override void OnDestroyed()
        {
            AtmosRenderContext.ClearSnapshot();
            _atlas?.Dispose();
            _atlas = null;
        }

        public void ApplyChunkPatch(AtmosChunkPatch patch)
        {
            if (AtmosRenderContext.IsDebugSnapshotOverrideEnabled() || _atlas == null)
                return;

            _atlas.ApplyChunkPatch(patch);
            if (!_atlas.IsValid)
                return;

            GasVisualProfileBuilder.GpuSet gasProfiles =
                GasVisualProfileBuilder.Build(_atmos != null ? _atmos.GasRegistry : null);
            AtmosRenderContext.SetSnapshot(_atlas.BuildSnapshot(gasProfiles));
        }
    }
}
