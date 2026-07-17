using FishNet;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using SS3D.Core;
using SS3D.Systems.Entities.Events;
using SS3D.Rendering.URP;
using Coimbra;
using Coimbra.Services.Events;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// Client-side field-of-view producer. Drives <see cref="VisionOcclusionCapture"/> - a real GPU
    /// depth cubemap rendered from the player's position - and exposes the player's pose/cone to
    /// the URP vision render feature.
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
        public Transform target = null;

        [Space]
        [SerializeField]
        private float viewRange = 35;

        [Range(0, 360)]
        [SerializeField]
        [Tooltip("The field of view width")]
        private float viewConeWidth = 360;

        [SerializeField]
        [Tooltip("The center of the field of view's actual wall detection")]
        private Vector3 detectionOffset = Vector3.zero;

        // Defaults to "everything except known non-occluding layers" so the capture camera - which
        // sits at the player's own position - doesn't immediately self-occlude on the player's own
        // body mesh. Tune down further in the Inspector to just wall/structure layers if other
        // furniture/props end up occluding vision incorrectly.
        [SerializeField]
        [Tooltip("Layers the occlusion capture treats as vision-blocking geometry (walls, closed doors, ...). " +
                 "Must exclude Characters/BodyParts or the capture self-occludes on the player's own body.")]
        private LayerMask occluderMask = ~LayerMask.GetMask(
            "TransparentFX", "Ignore Raycast", "Water", "UI", "Items", "Characters", "BodyParts");

        private readonly VisionOcclusionCapture _occlusionCapture = new();

        static ProfilerMarker OcclusionCaptureMarker = new ProfilerMarker("Vision.OcclusionCapture");

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
        }

        protected override void OnDisabled()
        {
            VisionRenderContext.Enabled = false;
            _occlusionCapture.Dispose();

            _clientVisionInitialized = false;
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            target = e.PlayerObject.transform;
        }

        private void LateUpdate()
        {
            if (!_clientVisionInitialized || !target)
            {
                VisionRenderContext.Enabled = false;
                return;
            }

            transform.position = target.transform.position;
            float angle = target.transform.rotation.eulerAngles.y * Mathf.Deg2Rad;

            Shader.SetGlobalVector("_PlayerPos", DetectionCenter);
            Shader.SetGlobalFloat("_PlayerAngle", angle);
            Shader.SetGlobalFloat("_ViewConeWidth", viewConeWidth * Mathf.Deg2Rad);

            OcclusionCaptureMarker.Begin();
            _occlusionCapture.Capture(DetectionCenter, viewRange, occluderMask);
            OcclusionCaptureMarker.End();

            VisionRenderContext.Enabled = true;
        }
    }
}
