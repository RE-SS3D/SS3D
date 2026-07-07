using UnityEngine;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Per-tick atmospherics GPU snapshot consumed by <see cref="AtmosRendererFeature"/> (Phase 2+).
    /// </summary>
    public static class AtmosRenderContext
    {
        public struct Snapshot
        {
            public Texture2D Pressure;
            public Texture2D Temperature;
            public Texture2D Composition;
            public Texture2D Flow;
            public Texture2D FireIntensity;
            public Texture2D Mask;

            /// <summary>minX, minZ, width, height in world tile coordinates.</summary>
            public Vector4 AtlasBounds;
            public int MapId;
            public bool Valid;
            public float IgnitionTemperature;
        }

        static Snapshot? s_Snapshot;

        public static void SetSnapshot(Snapshot snapshot)
        {
            s_Snapshot = snapshot.Valid ? snapshot : null;
        }

        public static void ClearSnapshot()
        {
            s_Snapshot = null;
        }

        public static bool TryGetSnapshot(out Snapshot snapshot)
        {
            if (s_Snapshot.HasValue && s_Snapshot.Value.Valid)
            {
                snapshot = s_Snapshot.Value;
                return true;
            }

            snapshot = default;
            return false;
        }
    }
}
