using System;
using FishNet;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using SS3D.Core;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Tile;
using SS3D.Rendering.URP;
using Coimbra;
using Coimbra.Services.Events;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// Client-side field-of-view producer. Casts polar rays across the tile grid (via
    /// <see cref="VisionGridCaster"/> and <see cref="VisionOcclusionProvider"/>) to build the 1D
    /// <c>_VisionMap</c> depth texture consumed by the URP vision render feature.
    /// </summary>
    public class VisionSubSystem : Core.Behaviours.SubSystem
    {
        // Bootstraps itself instead of living in Boot.unity like the other persistent subsystems, since hand-editing
        // scene YAML outside the Unity Editor isn't safe. Mirrors ScreenEffectsSubSystem's bootstrap - move this
        // into Boot.unity later if preferred, the behaviour is identical, this is just how it gets into the scene
        // without an Editor session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SubSystems.TryGet(out VisionSubSystem _))
            {
                return;
            }

            GameObject host = new(nameof(VisionSubSystem));
            DontDestroyOnLoad(host);
            host.AddComponent<VisionSubSystem>();
        }

        [SerializeField]
        public bool showDebug;
        [SerializeField]
        private Texture2D visionMap;

        [SerializeField]
        public Transform target = null;

        [Space]
        [SerializeField]
        private float viewRange = 35;

        [Range(0, 360)]
        [SerializeField]
        [Tooltip("The field of view width")]
        private float viewConeWidth = 360;

        [SerializeField]
        [Tooltip("Samples per degree of the view cone")]
        private float resolution = 1f;

        [SerializeField]
        [Tooltip("The center of the field of view's actual wall detection")]
        private Vector3 detectionOffset = Vector3.zero;

        [NonSerialized]
        public int stepCount;

        private float[] _depthBuffer;
        private Color[] _pixelBuffer;

        private TileSubSystem _tileSubSystem;
        private VisionOcclusionProvider _occlusion;
        private ITileQueryService _query;

        static ProfilerMarker GridCastMarker = new ProfilerMarker("Vision.GridCast");
        static ProfilerMarker MapUploadMarker = new ProfilerMarker("Vision.CacheUpload");

        private bool _clientVisionInitialized;

        private Vector3 DetectionCenter => target.transform.position + detectionOffset;

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(LocalPlayerObjectChanged.AddListener(HandlePlayerObjectChanged));
        }

        protected override void OnEnabled()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                gameObject.Dispose(true);
                return;
            }

            if (InstanceFinder.IsServerOnly)
            {
                return;
            }

            _clientVisionInitialized = true;

            stepCount = Mathf.Max(1, Mathf.CeilToInt(viewConeWidth * resolution));
            visionMap = new Texture2D(stepCount, 1, TextureFormat.R16, false);
            visionMap.wrapMode = TextureWrapMode.Repeat;
            visionMap.filterMode = FilterMode.Bilinear;
            Shader.SetGlobalTexture("_VisionMap", visionMap);

            _depthBuffer = new float[stepCount];
            _pixelBuffer = new Color[stepCount];

            _tileSubSystem = SubSystems.Get<TileSubSystem>();
            TryBindMap();
        }

        protected override void OnDisabled()
        {
            VisionRenderContext.Enabled = false;

            if (_tileSubSystem != null && _occlusion != null)
                _tileSubSystem.UnregisterTileMutationObserver(_occlusion);

            _occlusion = null;
            _query = null;

            _clientVisionInitialized = false;
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            target = e.PlayerObject.transform;
        }

        /// <summary>
        /// Binds to the tilemap once it becomes available (already present when hosting, created shortly
        /// after connect on a remote client) and registers the occlusion cache as a mutation observer.
        /// </summary>
        private void TryBindMap()
        {
            if (_occlusion != null || _tileSubSystem == null)
                return;

            TileMap map = _tileSubSystem.CurrentMap;
            ITileQueryService query = _tileSubSystem.QueryService;
            if (map == null || query == null)
                return;

            _query = query;
            _occlusion = new VisionOcclusionProvider(map, query);
            _tileSubSystem.RegisterTileMutationObserver(_occlusion);
        }

        private void LateUpdate()
        {
            if (_occlusion == null)
                TryBindMap();

            if (!_clientVisionInitialized || !target || _occlusion == null || _query == null)
            {
                VisionRenderContext.Enabled = false;
                return;
            }

            transform.position = target.transform.position;
            float angle = target.transform.rotation.eulerAngles.y * Mathf.Deg2Rad;

            Shader.SetGlobalVector("_PlayerPos", DetectionCenter);
            Shader.SetGlobalFloat("_PlayerAngle", angle);
            Shader.SetGlobalFloat("_ViewConeWidth", viewConeWidth * Mathf.Deg2Rad);
            Shader.SetGlobalFloat("_ViewRange", viewRange);

            DrawVisionMap(angle);

            VisionRenderContext.Enabled = true;
        }

        private void DrawVisionMap(float yawRadians)
        {
            GridCastMarker.Begin();
            VisionGridCaster.Cast(
                _occlusion,
                _query,
                DetectionCenter,
                yawRadians,
                viewRange,
                viewConeWidth,
                stepCount,
                _depthBuffer);
            GridCastMarker.End();

            MapUploadMarker.Begin();
            for (int i = 0; i < stepCount; i++)
            {
                _pixelBuffer[i] = new Color(_depthBuffer[i], 0f, 0f);
            }

#pragma warning disable UNT0017 // SetPixels invocation is slow
            visionMap.SetPixels(_pixelBuffer);
#pragma warning restore UNT0017 // SetPixels invocation is slow
            visionMap.Apply(false);
            MapUploadMarker.End();
        }
    }
}
