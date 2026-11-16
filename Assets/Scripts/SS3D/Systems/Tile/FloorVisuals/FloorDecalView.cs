using Coimbra;
using SS3D.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.FloorVisuals
{
    /// <summary>
    /// Client/host mesh view for sparse floor decals stored on <see cref="TileChunk"/>.
    /// </summary>
    public sealed class FloorDecalView : MonoBehaviour
    {
        /// <summary>The one live view, or null before cold start.</summary>
        public static FloorDecalView Instance { get; private set; }

        private readonly List<GameObject> _spawned = new();
        private readonly Dictionary<ushort, Material> _materials = new();
        private readonly Dictionary<Vector2Int, ushort[]> _clientChunkCache = new();
        private TileMap _map;
        private Transform _root;
        private bool _bound;
        private Vector2Int? _lastAoiCell;

        public void ApplyClientChunk(Vector2Int chunkKey, ushort[] decalIds)
        {
            if (decalIds == null || decalIds.Length == 0)
                _clientChunkCache.Remove(chunkKey);
            else
            {
                var copy = new ushort[decalIds.Length];
                System.Array.Copy(decalIds, copy, decalIds.Length);
                _clientChunkCache[chunkKey] = copy;
            }

            Rebuild();
        }

        public void ReplaceClientChunks(IReadOnlyList<(Vector2Int chunkKey, ushort[] decalIds)> chunks)
        {
            _clientChunkCache.Clear();
            if (chunks != null)
            {
                foreach ((Vector2Int chunkKey, ushort[] decalIds) in chunks)
                    ApplyClientChunk(chunkKey, decalIds);
            }

            Rebuild();
        }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable() => TryBind();

        private void Update()
        {
            if (!_bound)
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
            if (_map != null)
                _map.OnFloorDecalsChanged -= HandleChunkDirty;

            _bound = false;
            ClearSpawned();
        }

        private void TryBind()
        {
            if (!SubSystems.TryGet(out TileSubSystem tileSubSystem) || tileSubSystem.CurrentMap == null)
                return;

            if (_map != tileSubSystem.CurrentMap)
            {
                if (_map != null)
                    _map.OnFloorDecalsChanged -= HandleChunkDirty;

                _map = tileSubSystem.CurrentMap;
                _map.OnFloorDecalsChanged += HandleChunkDirty;
                _bound = true;
                Rebuild();
            }
        }

        private void HandleChunkDirty(Vector2Int _) => Rebuild();

        private void EnsureRoot()
        {
            if (_root != null)
                return;

            var go = new GameObject("FloorDecals");
            _root = go.transform;
            _root.SetParent(transform, false);
        }

        private void Rebuild()
        {
            ClearSpawned();
            EnsureRoot();

            FloorDecalCatalog catalog = FloorDecalCatalog.Get();
            if (catalog == null)
                return;

            if (_map != null && FishNet.InstanceFinder.IsServer)
            {
                foreach (TileChunk chunk in _map.GetAllChunks())
                {
                    Vector2Int chunkKey = _map.GetKey(chunk.GetWorldPosition(0, 0));
                    if (!TileAoiVisibility.IsChunkInLocalAoi(chunkKey, TileAoiVisibility.OverlayChunkPad))
                        continue;

                    ushort[] ids = chunk.CopyFloorDecalIds();
                    if (ids == null)
                        continue;

                    SpawnChunk(catalog, chunkKey, ids);
                }

                return;
            }

            foreach (KeyValuePair<Vector2Int, ushort[]> pair in _clientChunkCache)
            {
                if (!TileAoiVisibility.IsChunkInLocalAoi(pair.Key, TileAoiVisibility.OverlayChunkPad))
                    continue;

                SpawnChunk(catalog, pair.Key, pair.Value);
            }
        }

        private void SpawnChunk(FloorDecalCatalog catalog, Vector2Int chunkKey, ushort[] ids)
        {
            Vector3 origin = new Vector3(chunkKey.x * TileConstants.ChunkSize, 0f, chunkKey.y * TileConstants.ChunkSize);

            for (int y = 0; y < TileConstants.ChunkSize; y++)
            {
                for (int x = 0; x < TileConstants.ChunkSize; x++)
                {
                    ushort id = ids[y * TileConstants.ChunkSize + x];
                    if (id == 0 || !catalog.TryGet(id, out FloorDecalDefinition definition))
                        continue;

                    Material material = GetOrCreateMaterial(definition);
                    if (material == null)
                        continue;

                    Vector3 world = origin + new Vector3(x, 0f, y);
                    GameObject decal = FloorVisualMesh.CreateQuadObject(
                        $"Decal_{id}_{x}_{y}",
                        _root,
                        world,
                        material);
                    _spawned.Add(decal);
                }
            }
        }

        private Material GetOrCreateMaterial(FloorDecalDefinition definition)
        {
            if (_materials.TryGetValue(definition.Id, out Material existing) && existing != null)
                return existing;

            Material material = definition.MaterialOverride != null
                ? new Material(definition.MaterialOverride)
                : FloorVisualMesh.CreateCutoutMaterial(definition.Texture, definition.Tint);

            if (definition.MaterialOverride != null)
                material.color = definition.Tint;

            _materials[definition.Id] = material;
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

            var go = new GameObject(nameof(FloorDecalView));
            DontDestroyOnLoad(go);
            go.AddComponent<FloorDecalView>();
        }
    }
}
