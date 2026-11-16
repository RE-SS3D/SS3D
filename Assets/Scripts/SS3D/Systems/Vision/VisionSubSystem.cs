using System;
using FishNet;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Tile.Connections;
using SS3D.Rendering.URP;
using Coimbra;
using Coimbra.Services.Events;

namespace SS3D.Systems.Vision
{
    /// <summary>Whether the client field of view is drawn (2026-11_fov-rework D6).</summary>
    public enum VisionState
    {
        /// <summary>No living body to see from (ghost, lobby, server), or not set up yet.</summary>
        Off,

        /// <summary>A meta view (the map editor) has the screen; everything shows.</summary>
        Suppressed,

        /// <summary>The mask is drawn from the local body's eyes.</summary>
        Active,
    }

    /// <summary>
    /// Client-side field of view (fov.md, 2026-11_fov-rework). Each frame casts one batch of level rays
    /// from <see cref="Entity.ViewPoint"/> against the <see cref="VisionOccluder.LayerName"/> layer, then
    /// writes the hit distances into the polar <c>_VisionMap</c> and the wall tiles they hit into
    /// <c>_VisionTiles</c>. <c>VisionRendererFeature</c> draws the mask from those; <see cref="IsVisible"/>
    /// answers the same question for gameplay. The only owner of the vision state.
    /// </summary>
    public class VisionSubSystem : Core.Behaviours.SubSystem
    {
        // Bootstrapped by SystemsBootstrap (process-wide DDOL). Scene VisionSystem GO must not
        // also exist — dual registration fights SubSystems.Register.

        /// <summary>Slack for rounding between the polar map and a pixel's distance, not a reach into walls.</summary>
        public const float DepthEpsilon = 0.02f;

        /// <summary>Within this radius everything shows, even outside a narrow cone.</summary>
        public const float NearBubble = 1f;

        /// <summary>
        /// Above the body's feet by this much, a point inside a wall tile a ray hit shows whole (D1); below it,
        /// the floor of that tile keeps the depth test so nothing leaks past a door's leaves.
        /// </summary>
        public const float WallMinHeight = 0.08f;

        private static readonly int VisionMapId = Shader.PropertyToID("_VisionMap");
        private static readonly int VisionMapTexelSizeId = Shader.PropertyToID("_VisionMap_TexelSize");
        private static readonly int VisionTilesId = Shader.PropertyToID("_VisionTiles");
        private static readonly int VisionTileOriginId = Shader.PropertyToID("_VisionTileOrigin");
        private static readonly int PlayerPosId = Shader.PropertyToID("_PlayerPos");
        private static readonly int PlayerAngleId = Shader.PropertyToID("_PlayerAngle");
        private static readonly int ViewConeWidthId = Shader.PropertyToID("_ViewConeWidth");
        private static readonly int ViewRangeId = Shader.PropertyToID("_ViewRange");
        private static readonly int DepthEpsilonId = Shader.PropertyToID("_VisionDepthEpsilon");
        private static readonly int NearBubbleId = Shader.PropertyToID("_VisionNearBubble");
        private static readonly int WallMinYId = Shader.PropertyToID("_VisionWallMinY");

        private static readonly ProfilerMarker CastMarker = new("Vision.ViewPoints");
        private static readonly ProfilerMarker MapMarker = new("Vision.VisionMap");

        [SerializeField]
        public bool showDebug;

        [SerializeField]
        public Transform target;

        [SerializeField]
        private float viewRange = 35;

        [Range(0, 360)]
        [SerializeField]
        [Tooltip("The field of view width")]
        private float viewConeWidth = 360;

        [SerializeField]
        [Tooltip("Rays per degree of the view cone.")]
        private float resolution = 3f;

        /// <summary>Ray hit points from the last completed cast (editor gizmo, scenarios).</summary>
        [NonSerialized]
        public NativeArray<Vector3> viewPoints;

        [NonSerialized]
        public int stepCount;

        private Entity _targetEntity;
        private bool _clientVisionInitialized;
        private bool _suppressed;

        private NativeArray<RaycastCommand> _rayCommands;
        private NativeArray<RaycastHit> _rayHits;
        private JobHandle _castHandle;
        private bool _castPending;
        private Vector3 _pendingCenter;
        private float _pendingYaw;
        private float _pendingFloorY;

        // Written by VisionHitsJob; copied to the managed arrays IsVisible reads once the cast completes.
        private NativeArray<float> _jobDepths;
        private NativeArray<ushort> _jobDepthUpload;
        private NativeArray<byte> _jobSeenTiles;
        private NativeArray<Vector3> _jobPoints;

        private Texture2D _visionMap;
        private float[] _depths;

        private Texture2D _tileMap;
        private byte[] _seenTiles;
        private int _tileSize;
        private Vector2Int _pendingTileOrigin;

        // Pose of the completed cast behind _depths and _seenTiles, for IsVisible between casts.
        private bool _hasCast;
        private Vector3 _castCenter;
        private float _castYaw;
        private float _castFloorY;
        private Vector2Int _castTileOrigin;

        public VisionState State { get; private set; }

        public float ViewRange => viewRange;

        /// <summary>World-space origin used for casts and the shader's <c>_PlayerPos</c>.</summary>
        public Vector3 DetectionCenter
        {
            get
            {
                if (_targetEntity != null && _targetEntity.ViewPoint != null)
                    return _targetEntity.ViewPoint.transform.position;

                return target != null ? target.position + (Vector3.up * VisionOccluder.EyeHeight) : Vector3.zero;
            }
        }

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
                return;

            _clientVisionInitialized = true;
            EnsureBuffers(Mathf.Max(1, Mathf.CeilToInt(viewConeWidth * resolution)));
            RenderPipelineManager.beginContextRendering += HandleBeginContextRendering;
        }

        protected override void OnDisabled()
        {
            RenderPipelineManager.beginContextRendering -= HandleBeginContextRendering;
            _castHandle.Complete();
            _castPending = false;
            SetState(VisionState.Off);
            DisposeBuffers();

            if (_visionMap != null)
                Destroy(_visionMap);
            if (_tileMap != null)
                Destroy(_tileMap);

            _visionMap = null;
            _tileMap = null;
            _clientVisionInitialized = false;
        }

        /// <summary>Pause the field of view for a meta view (the map editor). The caller clears it on exit.</summary>
        public void SetSuppressed(bool suppressed)
        {
            _suppressed = suppressed;
            UpdateState();
        }

        /// <summary>
        /// False when the mask hides <paramref name="worldPoint"/>: the same rule and depths the shader uses
        /// (D1), so gameplay agrees with the screen. Always true with no mask showing: ghosts, the map
        /// editor, and before the first cast.
        /// </summary>
        public bool IsVisible(Vector3 worldPoint)
        {
            if (State != VisionState.Active || !_hasCast)
                return true;

            return IsWithinDepths(_depths, stepCount, viewRange, viewConeWidth, _castCenter, _castYaw, worldPoint)
                || IsPartOfSeenWall(_seenTiles, _tileSize, _castTileOrigin, _castFloorY, worldPoint);
        }

        /// <summary>
        /// CPU side of the wall rule in <c>VisionIsVisibleWorld</c> (D1): a point above the floor inside a tile
        /// a ray hit, given the cast's seen-tile grid (<paramref name="size"/> square, <paramref name="origin"/>
        /// the world tile of entry 0).
        /// </summary>
        public static bool IsPartOfSeenWall(byte[] seenTiles, int size, Vector2Int origin, float floorY, Vector3 worldPoint)
        {
            if (worldPoint.y <= floorY + WallMinHeight)
                return false;

            int x = Mathf.FloorToInt(worldPoint.x + 0.5f) - origin.x;
            int z = Mathf.FloorToInt(worldPoint.z + 0.5f) - origin.y;
            return x >= 0 && z >= 0 && x < size && z < size && seenTiles[x + (z * size)] != 0;
        }

        /// <summary>
        /// Distance the last cast's ray nearest to <paramref name="direction"/> travelled before a sight
        /// blocker, or the view range. For scenarios.
        /// </summary>
        public float DepthAlong(Vector3 direction)
        {
            if (!_hasCast)
                return viewRange;

            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float fromConeStart = Mathf.Repeat(angle - _castYaw + (viewConeWidth * 0.5f), 360f);
            int bin = Mathf.Clamp(Mathf.FloorToInt(fromConeStart / viewConeWidth * stepCount), 0, stepCount - 1);
            return _depths[bin] * viewRange;
        }

        /// <summary>
        /// CPU side of the depth test in <c>VisionIsVisibleWorld</c> (Vision.hlsl): the bin holding the
        /// point's angle, point-sampled like the shader's map.
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

            if (fromConeStart >= coneWidth)
                return distance <= NearBubble;

            int bin = Mathf.Clamp(Mathf.FloorToInt(fromConeStart / coneWidth * count), 0, count - 1);
            return distance <= (depths[bin] * range) + DepthEpsilon;
        }

        /// <summary>The tile a ray hit: the one just inside the surface, so a hit on a tile edge lands in the wall, not on the floor in front of it.</summary>
        public static Vector2Int HitTile(Vector3 point, Vector3 normal)
        {
            Vector3 inside = point - (normal * 0.01f);
            return new Vector2Int(Mathf.FloorToInt(inside.x + 0.5f), Mathf.FloorToInt(inside.z + 0.5f));
        }

        public Vector3 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal && target != null)
                angleInDegrees += target.eulerAngles.y;

            return Quaternion.AngleAxis(angleInDegrees, Vector3.up) * Vector3.forward;
        }

        private void HandlePlayerObjectChanged(ref EventContext context, in LocalPlayerObjectChanged e)
        {
            _targetEntity = e.PlayerObject != null ? e.PlayerObject.GetComponent<Entity>() : null;
            target = _targetEntity != null && _targetEntity.GetComponent<HumanoidGhostController>() == null
                ? _targetEntity.transform : null;
            UpdateState();
        }

        private void UpdateState()
        {
            if (!_clientVisionInitialized || target == null)
                SetState(VisionState.Off);
            else
                SetState(_suppressed ? VisionState.Suppressed : VisionState.Active);
        }

        private void SetState(VisionState state)
        {
            if (state != VisionState.Active)
            {
                _hasCast = false;
                Shader.SetGlobalFloat(ViewRangeId, 0f);
                Shader.SetGlobalTexture(VisionMapId, Texture2D.blackTexture);
            }

            State = state;
            VisionRenderContext.Publish(state == VisionState.Active);
        }

        private void LateUpdate()
        {
            UpdateState();
            if (State != VisionState.Active)
            {
                _castHandle.Complete();
                _castPending = false;
                return;
            }

            // A frame that never rendered (minimised, no camera) leaves its cast for this one.
            CompleteCast();

            using (CastMarker.Auto())
                ScheduleCast();
        }

        private void HandleBeginContextRendering(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
        {
            CompleteCast();
        }

        private void ScheduleCast()
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(viewConeWidth * resolution));
            if (count != stepCount)
                EnsureBuffers(count);

            Vector3 center = DetectionCenter;
            float yaw = target.eulerAngles.y;
            float stepAngle = viewConeWidth / stepCount;
            int half = (_tileSize - 1) / 2;
            Vector2Int tileOrigin = new(Mathf.FloorToInt(center.x + 0.5f) - half, Mathf.FloorToInt(center.z + 0.5f) - half);

            VisionRayCommandsJob commands = new()
            {
                Center = center,
                FirstAngle = (yaw - (viewConeWidth * 0.5f) + (stepAngle * 0.5f)) * Mathf.Deg2Rad,
                StepAngle = stepAngle * Mathf.Deg2Rad,
                Range = viewRange,
                Query = new QueryParameters(VisionOccluder.Mask, false, QueryTriggerInteraction.Ignore, false),
                Commands = _rayCommands,
            };

            VisionHitsJob hits = new()
            {
                Center = center,
                Range = viewRange,
                TileOrigin = tileOrigin,
                TileSize = _tileSize,
                Commands = _rayCommands,
                Hits = _rayHits,
                Depths = _jobDepths,
                DepthUpload = _jobDepthUpload,
                Points = _jobPoints,
                SeenTiles = _jobSeenTiles,
            };

            JobHandle filled = commands.Schedule(stepCount, 64);
            JobHandle cast = RaycastCommand.ScheduleBatch(_rayCommands, _rayHits, 32, 1, filled);
            _castHandle = hits.Schedule(cast);
            JobHandle.ScheduleBatchedJobs();

            _castPending = true;
            _pendingCenter = center;
            _pendingYaw = yaw;
            _pendingFloorY = target.position.y;
            _pendingTileOrigin = tileOrigin;

            Shader.SetGlobalVector(PlayerPosId, center);
            Shader.SetGlobalFloat(PlayerAngleId, yaw * Mathf.Deg2Rad);
            Shader.SetGlobalFloat(ViewConeWidthId, viewConeWidth * Mathf.Deg2Rad);
            Shader.SetGlobalFloat(ViewRangeId, viewRange);
            Shader.SetGlobalFloat(DepthEpsilonId, DepthEpsilon);
            Shader.SetGlobalFloat(NearBubbleId, NearBubble);
            Shader.SetGlobalFloat(WallMinYId, _pendingFloorY + WallMinHeight);
            Shader.SetGlobalVector(VisionTileOriginId, new Vector4(tileOrigin.x, tileOrigin.y, _tileSize, 0f));
        }

        private void CompleteCast()
        {
            if (!_castPending)
                return;

            using (MapMarker.Auto())
            {
                _castHandle.Complete();
                _castPending = false;

                _jobDepths.CopyTo(_depths);
                _jobPoints.CopyTo(viewPoints);
                _jobSeenTiles.CopyTo(_seenTiles);
                _visionMap.SetPixelData(_jobDepthUpload, 0);
                _visionMap.Apply(false);
                _tileMap.SetPixelData(_jobSeenTiles, 0);
                _tileMap.Apply(false);
                Shader.SetGlobalTexture(VisionMapId, _visionMap);
                Shader.SetGlobalVector(VisionMapTexelSizeId, new Vector4(1f / stepCount, 1f, stepCount, 1f));
                Shader.SetGlobalTexture(VisionTilesId, _tileMap);

                _castCenter = _pendingCenter;
                _castYaw = _pendingYaw;
                _castFloorY = _pendingFloorY;
                _castTileOrigin = _pendingTileOrigin;
                _hasCast = true;
            }
        }

        private void EnsureBuffers(int count)
        {
            _castHandle.Complete();
            _castPending = false;
            _hasCast = false;
            DisposeBuffers();

            stepCount = count;
            viewPoints = new NativeArray<Vector3>(count, Allocator.Persistent);
            _rayCommands = new NativeArray<RaycastCommand>(count, Allocator.Persistent);
            _rayHits = new NativeArray<RaycastHit>(count, Allocator.Persistent);
            _jobDepths = new NativeArray<float>(count, Allocator.Persistent);
            _jobPoints = new NativeArray<Vector3>(count, Allocator.Persistent);
            _jobDepthUpload = new NativeArray<ushort>(count, Allocator.Persistent);
            _depths = new float[count];

            if (_visionMap != null)
                Destroy(_visionMap);

            _visionMap = new Texture2D(count, 1, TextureFormat.R16, false, true)
            {
                name = "VisionMap",
                wrapMode = TextureWrapMode.Repeat,
                // Point: the shader reads the bin a pixel's angle falls in, exactly as IsWithinDepths does.
                filterMode = FilterMode.Point,
            };

            // One texel per tile, covering every tile a ray can reach.
            _tileSize = (2 * (Mathf.CeilToInt(viewRange) + 1)) + 1;
            _seenTiles = new byte[_tileSize * _tileSize];
            _jobSeenTiles = new NativeArray<byte>(_seenTiles.Length, Allocator.Persistent);
            if (_tileMap == null || _tileMap.width != _tileSize)
            {
                if (_tileMap != null)
                    Destroy(_tileMap);

                _tileMap = new Texture2D(_tileSize, _tileSize, TextureFormat.R8, false, true)
                {
                    name = "VisionTiles",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point,
                };
            }
        }

        private void DisposeBuffers()
        {
            if (viewPoints.IsCreated)
                viewPoints.Dispose();
            if (_rayCommands.IsCreated)
                _rayCommands.Dispose();
            if (_rayHits.IsCreated)
                _rayHits.Dispose();
            if (_jobDepths.IsCreated)
                _jobDepths.Dispose();
            if (_jobPoints.IsCreated)
                _jobPoints.Dispose();
            if (_jobDepthUpload.IsCreated)
                _jobDepthUpload.Dispose();
            if (_jobSeenTiles.IsCreated)
                _jobSeenTiles.Dispose();
        }
    }
}
