using UnityEngine;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Per-tick atmospherics GPU snapshot and per-camera render requests consumed by
    /// <see cref="AtmosRendererFeature"/>.
    /// </summary>
    public static class AtmosRenderContext
    {
        public struct Request
        {
            public Camera SourceCamera;
        }

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
            public Vector4[] GasScatter;
            public Vector4[] GasEmission;
            public Vector4[] GasMisc;
        }

        static Request? s_Request;
        static Snapshot? s_Snapshot;
        static bool s_DebugSnapshotOverride;

        public static void SetRequest(Request request)
        {
            s_Request = request.SourceCamera != null ? request : null;
        }

        public static void ClearRequest()
        {
            s_Request = null;
        }

        public static bool TryGetRequest(out Request request)
        {
            if (s_Request.HasValue && s_Request.Value.SourceCamera != null)
            {
                request = s_Request.Value;
                return true;
            }

            request = default;
            return false;
        }

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

        public static void SetDebugSnapshotOverride(bool enabled)
        {
            s_DebugSnapshotOverride = enabled;
        }

        public static bool IsDebugSnapshotOverrideEnabled()
        {
            return s_DebugSnapshotOverride;
        }
    }
}
