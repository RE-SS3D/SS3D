using Coimbra;
using SS3D.Core;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.FloorVisuals;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// Client/host-only floor corner stripes driven by departmental area tint + per-tile area ids.
    /// </summary>
    public sealed class AreaFloorStripeView : MonoBehaviour
    {
        /// <summary>The one live view, or null before cold start.</summary>
        public static AreaFloorStripeView Instance { get; private set; }

        private AreaSubSystem _areaSubSystem;
        private Transform _root;
        private readonly List<GameObject> _spawned = new();
        private readonly Dictionary<Color, Material> _materialsByTint = new();
        private bool _subscribed;
        private Vector2Int? _lastAoiCell;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            TryBind();
        }

        private void Update()
        {
            if (!_subscribed)
                TryBind();
            else if (TileAoiVisibility.TryGetLocalHashGridCell(out Vector2Int cell)
                     && (!_lastAoiCell.HasValue || _lastAoiCell.Value != cell))
            {
                _lastAoiCell = cell;
                Rebuild();
            }
        }

        private void OnDisable()
        {
            Unbind();
            ClearSpawned();
        }

        private void TryBind()
        {
            if (!SubSystems.TryGet(out AreaSubSystem areaSubSystem))
                return;

            _areaSubSystem = areaSubSystem;
            if (_subscribed)
                return;

            _areaSubSystem.FloorVisualCache.OnDirty += Rebuild;
            _subscribed = true;
            Rebuild();
        }

        private void Unbind()
        {
            if (!_subscribed || _areaSubSystem == null)
                return;

            _areaSubSystem.FloorVisualCache.OnDirty -= Rebuild;
            _subscribed = false;
        }

        private void EnsureRoot()
        {
            if (_root != null)
                return;

            var go = new GameObject("AreaFloorStripes");
            _root = go.transform;
            _root.SetParent(transform, false);
        }

        private void Rebuild()
        {
            ClearSpawned();
            EnsureRoot();

            if (_areaSubSystem == null)
                return;

            AreaFloorVisualCache cache = _areaSubSystem.FloorVisualCache;
            Texture2D texture = FloorVisualMesh.GetStripeCornerTexture();
            if (texture == null)
                return;

            foreach (KeyValuePair<Vector2Int, ushort[]> pair in cache.EnumerateChunks())
            {
                Vector2Int chunkKey = pair.Key;
                if (!TileAoiVisibility.IsChunkInLocalAoi(chunkKey, TileAoiVisibility.OverlayChunkPad))
                    continue;

                ushort[] ids = pair.Value;
                Vector3 origin = new Vector3(chunkKey.x * TileConstants.ChunkSize, 0f, chunkKey.y * TileConstants.ChunkSize);

                for (int y = 0; y < TileConstants.ChunkSize; y++)
                {
                    for (int x = 0; x < TileConstants.ChunkSize; x++)
                    {
                        ushort areaId = ids[y * TileConstants.ChunkSize + x];
                        if (areaId == AreaId.None || !cache.TryGetTint(areaId, out Color tint))
                            continue;

                        Material material = GetOrCreateMaterial(texture, tint);
                        Vector3 world = origin + new Vector3(x, 0f, y);
                        GameObject stripe = FloorVisualMesh.CreateQuadObject(
                            $"Stripe_{areaId}_{x}_{y}",
                            _root,
                            world,
                            material);
                        _spawned.Add(stripe);
                    }
                }
            }
        }

        private Material GetOrCreateMaterial(Texture2D texture, Color tint)
        {
            if (_materialsByTint.TryGetValue(tint, out Material existing) && existing != null)
                return existing;

            Material material = FloorVisualMesh.CreateCutoutMaterial(texture, tint);
            _materialsByTint[tint] = material;
            return material;
        }

        private void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    _spawned[i].Dispose(true);
            }

            _spawned.Clear();
        }

        /// <summary>Creates the process-wide view; called by <c>SystemsBootstrap</c> at cold start.</summary>
        public static void EnsureInstance()
        {
            if (Instance != null)
                return;

            var go = new GameObject(nameof(AreaFloorStripeView));
            DontDestroyOnLoad(go);
            go.AddComponent<AreaFloorStripeView>();
        }
    }
}
