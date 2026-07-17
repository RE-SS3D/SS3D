using System;
using FishNet;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Tile;
using SS3D.Rendering.URP;
using Coimbra;
using Coimbra.Services.Events;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// Client-side field-of-view producer. Casts rays from the player's
    /// <see cref="Entity.ViewPoint"/> into a 1D polar <c>_VisionMap</c> depth texture
    /// consumed by the URP vision render feature.
    /// </summary>
    public class VisionSubSystem : Core.Behaviours.SubSystem
    {
        // Bootstraps itself instead of living in Boot.unity like the other persistent subsystems, since hand-editing
        // scene YAML outside the Unity Editor isn't safe. Mirrors ScreenEffectsSubSystem's bootstrap.
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

        // Left at 0 and filled in OnAwake: LayerMask.GetMask cannot run from a field initializer.
        [SerializeField]
        [Tooltip("Layers raycasts can hit. Hits are then filtered to walls/doors only so tall " +
                 "furniture (lockers, vendors, etc.) does not occlude. Exclude Characters/BodyParts/Items.")]
        private LayerMask obstacleMask;

        [SerializeField]
        [Tooltip("Raycasts per degree of the view cone. Higher reduces angular stripe jitter.")]
        private float resolution = 3f;

        /// <summary>
        /// Safety cap on how many non-occluder colliders a single ray may skip (furniture,
        /// props, etc.) before giving up.
        /// </summary>
        private const int MaxOccluderSkips = 64;

        [SerializeField]
        [Tooltip("Fallback cast origin offset when the target has no Entity.ViewPoint")]
        private Vector3 detectionOffset = Vector3.zero;

        [NonSerialized]
        public NativeArray<Vector3> viewPoints;

        [NonSerialized]
        public int stepCount;

        private ViewCastInfo[] _viewCastResults;
        private float[] _angleBuffer;
        private Color[] _pixelBuffer;
        private Entity _targetEntity;
        private int _wallsLayer = -1;

        private static readonly ProfilerMarker MapPerformanceMarker = new("Vision.VisionMap");
        private static readonly ProfilerMarker PointsPerformanceMarker = new("Vision.ViewPoints");

        private bool _clientVisionInitialized;

        /// <summary>World-space origin used for casts and the shader's <c>_PlayerPos</c>.</summary>
        public Vector3 DetectionCenter
        {
            get
            {
                if (_targetEntity != null && _targetEntity.ViewPoint != null)
                    return _targetEntity.ViewPoint.transform.position;

                return target != null ? target.position + detectionOffset : detectionOffset;
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            if (obstacleMask == 0)
            {
                obstacleMask = ~LayerMask.GetMask(
                    "TransparentFX", "Ignore Raycast", "Water", "UI", "Items", "Characters", "BodyParts");
            }

            _wallsLayer = LayerMask.NameToLayer("Walls");

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
            EnsureVisionMap(stepCount);
            EnsureBuffers(stepCount);
        }

        protected override void OnDisabled()
        {
            StopVisionRendering();

            if (viewPoints.IsCreated)
                viewPoints.Dispose();

            if (visionMap != null)
            {
                Destroy(visionMap);
                visionMap = null;
            }

            _clientVisionInitialized = false;
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            target = e.PlayerObject != null ? e.PlayerObject.transform : null;
            _targetEntity = e.PlayerObject != null ? e.PlayerObject.GetComponent<Entity>() : null;
        }

        private void LateUpdate()
        {
            if (!_clientVisionInitialized || !target)
            {
                StopVisionRendering();
                return;
            }

            transform.position = target.position;
            float angle = target.rotation.eulerAngles.y * Mathf.Deg2Rad;
            Vector3 center = DetectionCenter;

            Shader.SetGlobalVector("_PlayerPos", center);
            Shader.SetGlobalFloat("_PlayerAngle", angle);
            Shader.SetGlobalFloat("_ViewConeWidth", viewConeWidth * Mathf.Deg2Rad);
            Shader.SetGlobalFloat("_ViewRange", viewRange);

            DrawVisionMap(center);

            VisionRenderContext.Enabled = true;
        }

        /// <summary>
        /// Drops the URP composite and clears globals so a disabled/paused producer cannot
        /// leave a frozen FOV shadow on the Game camera.
        /// </summary>
        private static void StopVisionRendering()
        {
            VisionRenderContext.Enabled = false;
            Shader.SetGlobalFloat("_ViewRange", 0f);
            Shader.SetGlobalTexture("_VisionMap", Texture2D.blackTexture);
        }

        public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal && target != null)
                angleInDegrees += target.eulerAngles.y;

            return Quaternion.AngleAxis(angleInDegrees, Vector3.up) * Vector3.forward;
        }

        private void DrawVisionMap(Vector3 center)
        {
            PointsPerformanceMarker.Begin();
            CalculateViewPoints(center);
            PointsPerformanceMarker.End();

            MapPerformanceMarker.Begin();

            EnsureVisionMap(stepCount);

            for (int i = 0; i < stepCount; i++)
            {
                Vector3 offset = viewPoints[i] - center;
                offset.y = 0f;
                _pixelBuffer[i] = new Color(offset.magnitude / viewRange, 0f, 0f);
            }

            DilateSimilarDepthPixels();

#pragma warning disable UNT0017 // SetPixels invocation is slow
            visionMap.SetPixels(0, 0, stepCount, 1, _pixelBuffer);
#pragma warning restore UNT0017
            visionMap.Apply(false);

            MapPerformanceMarker.End();
        }

        private void CalculateViewPoints(Vector3 origin)
        {
            int newStepCount = Mathf.Max(1, Mathf.CeilToInt(viewConeWidth * resolution));
            if (newStepCount != stepCount)
            {
                stepCount = newStepCount;
                EnsureBuffers(stepCount);
            }

            float stepAngleSize = viewConeWidth / stepCount;
            float halfCone = viewConeWidth * 0.5f;
            float yaw = target.eulerAngles.y;

            // Sample bin centers so bilinear UV lookups align with ray angles (reduces
            // crawling vertical stripes when rotating past flat walls).
            for (int i = 0; i < stepCount; i++)
                _angleBuffer[i] = (yaw - halfCone) + (stepAngleSize * (i + 0.5f));

            ViewCastBatch(origin, _angleBuffer, stepCount, _viewCastResults);

            for (int i = 0; i < stepCount; i++)
                viewPoints[i] = _viewCastResults[i].Point;
        }

        /// <summary>
        /// Fill 1° angular holes on continuous surfaces without extending into distant corridors.
        /// </summary>
        private void DilateSimilarDepthPixels()
        {
            const float similarMeters = 1.75f;
            float similarNorm = similarMeters / viewRange;
            Color[] source = new Color[stepCount];
            Array.Copy(_pixelBuffer, source, stepCount);

            for (int i = 0; i < stepCount; i++)
            {
                float center = source[i].r;
                float left = source[(i - 1 + stepCount) % stepCount].r;
                float right = source[(i + 1) % stepCount].r;
                float maxNeighbor = Mathf.Max(left, right);
                if (Mathf.Abs(maxNeighbor - center) <= similarNorm)
                    _pixelBuffer[i].r = Mathf.Max(center, maxNeighbor);
            }
        }

        private void ViewCastBatch(Vector3 origin, float[] angles, int count, ViewCastInfo[] resultArray)
        {
            if (resultArray.Length < count)
                throw new ArgumentException("Results can't be smaller than count", nameof(resultArray));

            // Push past the collider hit so the occluder's own mesh (often slightly behind the
            // collider) stays visible. Without this, walls paint black while still correctly
            // hiding everything beyond them.
            const float occluderSurfaceBias = 0.75f;

            for (int i = 0; i < count; i++)
            {
                Vector3 direction = DirectionFromAngle(angles[i], true);
                if (TryFindNearestOccluder(origin, direction, out float hitDistance, out Vector3 hitNormal))
                {
                    float visibleDistance = Mathf.Min(hitDistance + occluderSurfaceBias, viewRange);
                    resultArray[i] = new ViewCastInfo(
                        true,
                        origin + (direction * visibleDistance),
                        visibleDistance,
                        angles[i],
                        hitNormal);
                }
                else
                {
                    resultArray[i] = new ViewCastInfo(
                        false,
                        origin + (direction * viewRange),
                        viewRange,
                        angles[i],
                        Vector3.zero);
                }
            }
        }

        /// <summary>
        /// Walk the ray, skipping furniture/props, until the nearest wall/door (or miss).
        /// Uses iterative single-hit casts so a dense prop pile cannot exhaust a fixed hit
        /// buffer and falsely report a clear line of sight through walls.
        /// </summary>
        private bool TryFindNearestOccluder(
            Vector3 origin,
            Vector3 direction,
            out float occluderDistance,
            out Vector3 occluderNormal)
        {
            const float skin = 0.05f;
            float traveled = 0f;
            Vector3 from = origin;

            for (int skip = 0; skip < MaxOccluderSkips; skip++)
            {
                float remaining = viewRange - traveled;
                if (remaining <= skin)
                    break;

                if (!Physics.Raycast(
                        from,
                        direction,
                        out RaycastHit hit,
                        remaining,
                        obstacleMask,
                        QueryTriggerInteraction.Ignore))
                {
                    break;
                }

                float distanceFromOrigin = traveled + hit.distance;
                if (IsVisionOccluder(hit.collider))
                {
                    occluderDistance = distanceFromOrigin;
                    occluderNormal = hit.normal;
                    return true;
                }

                // Advance past this non-occluder and keep searching for a wall/door.
                traveled = distanceFromOrigin + skin;
                from = origin + (direction * traveled);
            }

            occluderDistance = viewRange;
            occluderNormal = Vector3.zero;
            return false;
        }

        /// <summary>
        /// True for wall/door tile objects and anything on the Walls layer. Windows are
        /// intentionally see-through for FOV (same as SS13-style line of sight).
        /// </summary>
        private bool IsVisionOccluder(Collider collider)
        {
            // Proximity / interaction triggers (e.g. airlock open volume) must never darken FOV.
            if (collider.isTrigger)
                return false;

            if (_wallsLayer >= 0 && collider.gameObject.layer == _wallsLayer)
                return true;

            PlacedTileObject placed = collider.GetComponentInParent<PlacedTileObject>();
            if (placed == null)
                return false;

            return placed.GenericType switch
            {
                TileObjectGenericType.Door => true,
                TileObjectGenericType.Wall => !TileOccupancyEvaluator.IsWindow(placed),
                _ => false,
            };
        }

        private void EnsureVisionMap(int width)
        {
            if (visionMap != null && visionMap.width == width)
            {
                Shader.SetGlobalTexture("_VisionMap", visionMap);
                Shader.SetGlobalVector("_VisionMap_TexelSize", new Vector4(1f / width, 1f, width, 1f));
                return;
            }

            if (visionMap != null)
                Destroy(visionMap);

            visionMap = new Texture2D(width, 1, TextureFormat.R16, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                // Bilinear across neighbouring polar rays; point sampling caused vertical
                // black stripes on flat walls (angle quantization). See Vision.hlsl for a
                // similar-depth neighbor fill that limits corridor bleed at wall edges.
                filterMode = FilterMode.Bilinear,
            };
            Shader.SetGlobalTexture("_VisionMap", visionMap);
            Shader.SetGlobalVector("_VisionMap_TexelSize", new Vector4(1f / width, 1f, width, 1f));
        }

        private void EnsureBuffers(int count)
        {
            if (viewPoints.IsCreated)
                viewPoints.Dispose();

            viewPoints = new NativeArray<Vector3>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _viewCastResults = new ViewCastInfo[count];
            _angleBuffer = new float[count];
            _pixelBuffer = new Color[count];
        }

        public struct ViewCastInfo
        {
            public bool Hit;
            public Vector3 Point;
            public float Distance;
            public float Angle;
            public Vector3 Normal;

            public ViewCastInfo(bool hit, Vector3 point, float distance, float angle, Vector3 normal)
            {
                Hit = hit;
                Point = point;
                Distance = distance;
                Angle = angle;
                Normal = normal;
            }
        }
    }
}
