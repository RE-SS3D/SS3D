using UnityEngine;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Per-frame selection pick state consumed by <see cref="SelectionPickRendererFeature"/>.
    /// </summary>
    public static class SelectionPickContext
    {
        public struct Request
        {
            public Camera SourceCamera;
            public RenderTexture Target;
            public bool DebugView;
        }

        static Request? s_Request;

        public static void SetRequest(Request request)
        {
            s_Request = request;
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
    }
}
