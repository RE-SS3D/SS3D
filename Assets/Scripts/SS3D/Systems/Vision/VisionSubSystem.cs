using System;
using System.Collections.Generic;
using FishNet;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
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
        // Bootstrapped by SystemsBootstrap (process-wide DDOL). Scene VisionSystem GO must not
        // also exist — dual registration fights SubSystems.Register.

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
        /// Cap on wave iterations that skip non-occluder colliders (furniture, props).
        /// Each wave is one <see cref="RaycastCommand.ScheduleBatch"/> for all still-active rays.
        /// </summary>
        private const int MaxOccluderSkips = 64;

        /// <summary>Radial slack added in <c>VisionSampleWallDepth</c> (Vision.hlsl). Keep the two equal.</summary>
        public const float ShaderDepthSlack = 0.05f;

        // With ShaderDepthSlack, 0.45 m into the occluder, about its centre line. Colliders are far
        // thicker than what they draw: a wall's face is 0.28 m behind its near collider face and
        // an airlock's far face 0.57 m past it, so this lights near faces and hides far ones.
        private const float OccluderSlackPerpendicular = 0.4f;

        // Slack for a ray that glances along the wall, where a fixed depth would need a long ray.
        // Also the most any ray gets.
        private const float OccluderSlackMax = 1.1f;

        // A collider thinner than this along the hit normal is a jamb cap, not the wall's thickness.
        private const float ThinOccluder = 0.25f;

        [SerializeField]
        [Tooltip("Fallback cast origin offset when the target has no Entity.ViewPoint")]
        private Vector3 detectionOffset = Vector3.zero;

        [NonSerialized]
        public NativeArray<Vector3> viewPoints;

        [NonSerialized]
        public int stepCount;

        private ViewCastInfo[] _viewCastResults;
        private float[] _angleBuffer;
        /// <summary>Normalized view depth per angular bin (0–1), written into the R16 vision map.</summary>
        private float[] _depthBuffer;
        private float[] _dilateScratch;
        private ushort[] _depthUpload;
        private Entity _targetEntity;
        private int _wallsLayer = -1;

        // Batched Physics casts (one wave per prop-skip). Persistent to avoid per-frame TempJob alloc.
        private NativeArray<RaycastCommand> _rayCommands;
        private NativeArray<RaycastHit> _rayHits;
        private NativeArray<Vector3> _rayDirections;
        private float[] _rayTraveled;
        private int[] _activeRayIndices;

        /// <summary>
        /// Collider instance-id → occluder. Walls/doors are stable; cleared when oversized so
        /// recycled instance IDs cannot stick forever after destroy/respawn churn.
        /// </summary>
        private readonly Dictionary<int, bool> _occluderByColliderId = new(256);

        private const int OccluderCacheMaxEntries = 4096;

        private static readonly ProfilerMarker MapPerformanceMarker = new("Vision.VisionMap");
        private static readonly ProfilerMarker PointsPerformanceMarker = new("Vision.ViewPoints");

        private bool _clientVisionInitialized;

        // Pose of the rays behind _depthBuffer, for answering IsVisible between casts.
        private Vector3 _castCenter;
        private float _castYaw;

        /// <summary>
        /// When true, FOV composite is dropped (map editor / other meta views). Cleared by the owner.
        /// </summary>
        private bool _suppressed;

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
                    "TransparentFX", "Ignore Raycast", "Water", "UI", "Items", "Characters", "BodyParts",
                    StationBounds.CeilingLayerName);
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
            DisposeCastBuffers();

            if (viewPoints.IsCreated)
                viewPoints.Dispose();

            if (visionMap != null)
            {
                Destroy(visionMap);
                visionMap = null;
            }

            _occluderByColliderId.Clear();
            _clientVisionInitialized = false;
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            _targetEntity = e.PlayerObject != null ? e.PlayerObject.GetComponent<Entity>() : null;
            target = _targetEntity != null && _targetEntity.GetComponent<HumanoidGhostController>() == null
                ? _targetEntity.transform : null;
            if (target == null)
                StopVisionRendering();
        }

        private void LateUpdate()
        {
            if (VisionRenderContext.Suppressed || _suppressed || !_clientVisionInitialized || !target)
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
        /// Pause client FOV composite (e.g. map editor). Owner must clear on exit.
        /// </summary>
        public void SetSuppressed(bool suppressed)
        {
            _suppressed = suppressed;
            VisionRenderContext.Suppressed = suppressed;
            if (suppressed)
                StopVisionRendering();
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

        /// <summary>
        /// False when the FOV mask hides <paramref name="worldPoint"/>, using the depths the mask
        /// draws from so gameplay agrees with what is on screen. Always true with no mask showing:
        /// ghosts, the map editor, and before the first cast.
        /// </summary>
        public bool IsVisible(Vector3 worldPoint)
        {
            if (!VisionRenderContext.Enabled || _depthBuffer == null)
                return true;

            return IsWithinDepths(_depthBuffer, stepCount, viewRange, viewConeWidth, _castCenter, _castYaw, worldPoint);
        }

        /// <summary>
        /// CPU side of <c>VisionIsVisibleWorld</c> in Vision.hlsl. Reads the nearest bin rather than
        /// filtering, so the two can differ by a bin (a third of a degree) along a mask edge.
        /// </summary>
        public static bool IsWithinDepths(
            float[] depths,
            int count,
            float range,
            float coneWidth,
            Vector3 center,
            float yaw,
            Vector3 worldPoint)
        {
            Vector2 offset = new(worldPoint.x - center.x, worldPoint.z - center.z);
            float distance = offset.magnitude;

            float angle = Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg;
            float fromConeStart = Mathf.Repeat(angle - yaw + (coneWidth * 0.5f), 360f);

            // Same bubble as the shader: a point this close shows even outside a narrow cone.
            const float alwaysInConeDistance = 1f;
            if (fromConeStart >= coneWidth)
                return distance <= alwaysInConeDistance;

            int bin = Mathf.Clamp(Mathf.FloorToInt(fromConeStart / coneWidth * count), 0, count - 1);
            return distance <= (depths[bin] * range) + ShaderDepthSlack;
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
                _depthBuffer[i] = offset.magnitude / viewRange;
            }

            DilateSimilarDepthPixels();
            UploadVisionMap();

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
            _castCenter = origin;
            _castYaw = yaw;

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
        /// Reads from <see cref="_dilateScratch"/> so neighbouring bins use pre-dilate depths.
        /// </summary>
        private void DilateSimilarDepthPixels()
        {
            const float similarMeters = 1.75f;
            float similarNorm = similarMeters / viewRange;
            Array.Copy(_depthBuffer, _dilateScratch, stepCount);

            for (int i = 0; i < stepCount; i++)
            {
                float center = _dilateScratch[i];
                float left = _dilateScratch[(i - 1 + stepCount) % stepCount];
                float right = _dilateScratch[(i + 1) % stepCount];
                float maxNeighbor = Mathf.Max(left, right);
                if (Mathf.Abs(maxNeighbor - center) <= similarNorm)
                    _depthBuffer[i] = Mathf.Max(center, maxNeighbor);
            }
        }

        /// <summary>
        /// Pack normalized depths into the R16 texture without Color[] / SetPixels allocs.
        /// </summary>
        private void UploadVisionMap()
        {
            for (int i = 0; i < stepCount; i++)
            {
                float depth = Mathf.Clamp01(_depthBuffer[i]);
                _depthUpload[i] = (ushort)(depth * 65535f + 0.5f);
            }

            visionMap.SetPixelData(_depthUpload, 0);
            visionMap.Apply(false);
        }

        private void ViewCastBatch(Vector3 origin, float[] angles, int count, ViewCastInfo[] resultArray)
        {
            if (resultArray.Length < count)
                throw new ArgumentException("Results can't be smaller than count", nameof(resultArray));

            const float skin = 0.05f;

            QueryParameters query = new(obstacleMask, false, QueryTriggerInteraction.Ignore, false);

            for (int i = 0; i < count; i++)
            {
                float rad = angles[i] * Mathf.Deg2Rad;
                Vector3 direction = new(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
                _rayDirections[i] = direction;
                _rayTraveled[i] = 0f;
                _activeRayIndices[i] = i;
                resultArray[i] = new ViewCastInfo(
                    false,
                    origin + (direction * viewRange),
                    viewRange,
                    angles[i],
                    Vector3.zero);
            }

            int activeCount = count;

            // Wave schedule: all active rays cast together, then only non-occluder hits stay
            // active and advance. Same semantics as iterative single-hit Physics.Raycast, but
            // one ScheduleBatch per skip layer instead of ~stepCount sync casts.
            for (int skip = 0; skip < MaxOccluderSkips && activeCount > 0; skip++)
            {
                for (int a = 0; a < activeCount; a++)
                {
                    int ray = _activeRayIndices[a];
                    float remaining = viewRange - _rayTraveled[ray];
                    Vector3 from = origin + (_rayDirections[ray] * _rayTraveled[ray]);
                    _rayCommands[a] = new RaycastCommand(from, _rayDirections[ray], query, remaining);
                }

                NativeArray<RaycastCommand> commands = _rayCommands.GetSubArray(0, activeCount);
                NativeArray<RaycastHit> hits = _rayHits.GetSubArray(0, activeCount);
                RaycastCommand.ScheduleBatch(commands, hits, 32, 1, default(JobHandle)).Complete();

                int nextActive = 0;
                for (int a = 0; a < activeCount; a++)
                {
                    int ray = _activeRayIndices[a];
                    RaycastHit hit = hits[a];

                    // Miss (or zero-distance sentinel): leave the default full-range result.
                    if (hit.colliderInstanceID == 0)
                        continue;

                    float distanceFromOrigin = _rayTraveled[ray] + hit.distance;
                    Collider collider = hit.collider;
                    if (collider != null && IsVisionOccluder(collider))
                    {
                        Vector3 normal = OcclusionNormal(hit.normal, collider.bounds.size);
                        float slack = OccluderSlack(_rayDirections[ray], normal);
                        float visibleDistance = Mathf.Min(distanceFromOrigin + slack, viewRange);
                        resultArray[ray] = new ViewCastInfo(
                            true,
                            origin + (_rayDirections[ray] * visibleDistance),
                            visibleDistance,
                            angles[ray],
                            hit.normal);
                        continue;
                    }

                    // Advance past this non-occluder and keep searching for a wall/door.
                    _rayTraveled[ray] = distanceFromOrigin + skin;
                    if (viewRange - _rayTraveled[ray] > skin)
                        _activeRayIndices[nextActive++] = ray;
                }

                activeCount = nextActive;
            }
        }

        /// <summary>
        /// How far past a collider hit a ray stays lit, so the occluder's own mesh (well behind the
        /// collider) does not paint black. A fixed depth into the wall along
        /// <paramref name="occluderNormal"/>, so a glancing ray gets more length, which keeps the
        /// recessed faces of a wall you hug lit while what is behind it stays hidden.
        /// </summary>
        public static float OccluderSlack(Vector3 rayDirection, Vector3 occluderNormal)
        {
            float cosine = Mathf.Abs(Vector3.Dot(rayDirection, occluderNormal));
            if (cosine * OccluderSlackMax <= OccluderSlackPerpendicular)
                return OccluderSlackMax;

            return OccluderSlackPerpendicular / cosine;
        }

        /// <summary>
        /// The axis to measure a hit's depth along. A door jamb's cap is thin along the wall, so a
        /// ray hitting its side gets a normal that runs along the wall: measured that way, slack
        /// would either cross the wall behind it or black out the wall beside the door. Such a hit
        /// uses the other horizontal axis, the wall's thickness. Tile occluders are axis-aligned,
        /// so <paramref name="occluderSize"/> is the collider's world bounds.
        /// </summary>
        public static Vector3 OcclusionNormal(Vector3 hitNormal, Vector3 occluderSize)
        {
            bool alongX = Mathf.Abs(hitNormal.x) >= Mathf.Abs(hitNormal.z);
            float thickness = alongX ? occluderSize.x : occluderSize.z;
            if (thickness >= ThinOccluder)
                return hitNormal;

            return alongX ? Vector3.forward : Vector3.right;
        }

        /// <summary>
        /// True for wall/door tile objects and anything on the Walls layer. Windows are
        /// intentionally see-through for FOV (same as SS13-style line of sight).
        /// Results are cached by collider instance id — GetComponentInParent on ~1k rays
        /// dominates ViewPoints when uncached.
        /// </summary>
        private bool IsVisionOccluder(Collider collider)
        {
            int id = collider.GetInstanceID();
            if (_occluderByColliderId.TryGetValue(id, out bool cached))
                return cached;

            bool isOccluder = ClassifyVisionOccluder(collider);
            if (_occluderByColliderId.Count >= OccluderCacheMaxEntries)
                _occluderByColliderId.Clear();

            _occluderByColliderId[id] = isOccluder;
            return isOccluder;
        }

        private bool ClassifyVisionOccluder(Collider collider)
        {
            // Proximity / interaction triggers (e.g. airlock open volume) must never darken FOV.
            if (collider.isTrigger)
                return false;

            if (_wallsLayer >= 0 && collider.gameObject.layer == _wallsLayer)
                return true;

            PlacedTileObject placed = collider.GetComponentInParent<PlacedTileObject>();
            // Identity can still be unset on a pure client mid-spawn.
            if (placed == null || placed.tileObjectSO == null)
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

            DisposeCastBuffers();

            viewPoints = new NativeArray<Vector3>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _rayCommands = new NativeArray<RaycastCommand>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _rayHits = new NativeArray<RaycastHit>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _rayDirections = new NativeArray<Vector3>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _rayTraveled = new float[count];
            _activeRayIndices = new int[count];
            _viewCastResults = new ViewCastInfo[count];
            _angleBuffer = new float[count];
            _depthBuffer = new float[count];
            _dilateScratch = new float[count];
            _depthUpload = new ushort[count];
        }

        private void DisposeCastBuffers()
        {
            if (_rayCommands.IsCreated)
                _rayCommands.Dispose();
            if (_rayHits.IsCreated)
                _rayHits.Dispose();
            if (_rayDirections.IsCreated)
                _rayDirections.Dispose();
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
