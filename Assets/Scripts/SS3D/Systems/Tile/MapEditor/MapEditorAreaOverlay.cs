using System;
using Coimbra;
using System.Collections.Generic;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Client-local authoring overlay using the same membership and colors as area gizmos. A second instance built with a
    /// tint draws the same synced membership in colours its owner picks per room: the Power view's room tint
    /// (Documents/architecture/systems/map-editor-systems-views.md, Drawing), each room in its APC's circuit colour or hatched.
    /// </summary>
    public sealed class MapEditorAreaOverlay : IDisposable
    {
        /// <summary>How a tinting overlay draws one room's tiles.</summary>
        public readonly struct Tint : IEquatable<Tint>
        {
            public readonly bool Show;
            public readonly Color Color;
            /// <summary>Diagonal stripes in <see cref="Color"/> over a faint fill, instead of a flat fill.</summary>
            public readonly bool Hatched;

            public Tint(Color color, bool hatched = false)
            {
                Show = true;
                Color = color;
                Hatched = hatched;
            }

            public static Tint None => default;

            public bool Equals(Tint other) => Show == other.Show && Color == other.Color && Hatched == other.Hatched;
            public override bool Equals(object obj) => obj is Tint other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Show, Color, Hatched);
        }

        /// <summary>A flat tint's alpha and a hatched room's fill alpha (the stripes keep their colour's).</summary>
        public const float TintAlpha = 0.22f;
        public const float HatchFillAlpha = 0.12f;
        public const float HatchWidth = 0.07f;

        private readonly Func<ushort, Tint> _tint;
        private readonly int _renderQueue = -1;
        private readonly string _name = "MapEditorAreaOverlay";
        private readonly SystemsViewMeshBuilder _builder;
        // Tint mode: each chunk's renderer and room ids, and the chunks each room has tiles in, so a room whose tint
        // changed rebuilds only its own chunks.
        private readonly Dictionary<Vector2Int, MeshFilter> _chunkFilters = new();
        private readonly Dictionary<Vector2Int, ushort[]> _chunkIds = new();
        private readonly Dictionary<ushort, HashSet<Vector2Int>> _chunksOfArea = new();
        private readonly HashSet<Vector2Int> _dirtyChunks = new();
        private AreaFloorVisualCache _cache;
        private GameObject _root;
        private Material _material;
        private readonly List<Mesh> _meshes = new();
        private bool _dirty = true;
        private GameObject _paintPreview;
        private Mesh _paintPreviewMesh;

        public void ClearPaintPreview()
        {
            if (_paintPreview != null) _paintPreview.SetActive(false);
            if (_paintPreviewMesh != null) UnityEngine.Object.Destroy(_paintPreviewMesh);
            _paintPreviewMesh = null;
        }

        public void ShowPaintPreview(IReadOnlyList<Vector2Int> tiles, ushort areaId, bool erasing = false)
        {
            ClearPaintPreview();
            if (tiles == null || tiles.Count == 0 || _material == null) return;
            if (_paintPreview == null)
            {
                _paintPreview = new GameObject("AreaPaintPreview") { hideFlags = HideFlags.DontSave };
                _paintPreview.AddComponent<MeshFilter>();
                MeshRenderer renderer = _paintPreview.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            List<Vector3> vertices = new();
            List<Color> colors = new();
            List<int> triangles = new();
            Color color = erasing ? new Color(1f, 0.2f, 0.2f, 0.8f) :
                areaId == 0 ? new Color(0.2f, 0.85f, 0.45f, 0.8f) : AreaDevSettings.GetColorForArea(areaId);
            color.a = 0.8f;
            foreach (Vector2Int tile in tiles)
            {
                int start = vertices.Count;
                vertices.Add(new Vector3(tile.x - 0.48f, 0.25f, tile.y - 0.48f));
                vertices.Add(new Vector3(tile.x + 0.48f, 0.25f, tile.y - 0.48f));
                vertices.Add(new Vector3(tile.x - 0.48f, 0.25f, tile.y + 0.48f));
                vertices.Add(new Vector3(tile.x + 0.48f, 0.25f, tile.y + 0.48f));
                for (int v = 0; v < 4; v++) colors.Add(color);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                triangles.Add(start + 2); triangles.Add(start + 3); triangles.Add(start + 1);
            }
            _paintPreviewMesh = new Mesh { name = "AreaPaintPreview" };
            _paintPreviewMesh.SetVertices(vertices);
            _paintPreviewMesh.SetColors(colors);
            _paintPreviewMesh.SetTriangles(triangles, 0);
            _paintPreviewMesh.RecalculateBounds();
            _paintPreview.GetComponent<MeshFilter>().sharedMesh = _paintPreviewMesh;
            _paintPreview.SetActive(true);
        }
        public Dictionary<ushort, Vector3> LabelPositions { get; } = new();

        /// <summary>The Scripts › Rooms overlay: area gizmo colours.</summary>
        public MapEditorAreaOverlay() { }

        /// <summary>
        /// An overlay that draws each room as <paramref name="tint"/> says, at <paramref name="renderQueue"/> (under the
        /// systems view's runs at 4000). Call <see cref="Invalidate"/> when what the tint returns changes.
        /// </summary>
        public MapEditorAreaOverlay(Func<ushort, Tint> tint, int renderQueue, string name)
        {
            _tint = tint ?? throw new ArgumentNullException(nameof(tint));
            _renderQueue = renderQueue;
            _name = name;
            _builder = new SystemsViewMeshBuilder();
        }

        /// <summary>Rebuilds every chunk on the next visible frame, as a membership change does.</summary>
        public void Invalidate() => _dirty = true;

        /// <summary>
        /// Tint mode: these rooms' tints changed, so only the chunks they have tiles in rebuild on the next visible frame.
        /// </summary>
        public void InvalidateAreas(IEnumerable<ushort> areaIds)
        {
            if (_dirty || areaIds == null)
                return;

            foreach (ushort id in areaIds)
            {
                if (_chunksOfArea.TryGetValue(id, out HashSet<Vector2Int> chunks))
                    _dirtyChunks.UnionWith(chunks);
            }
        }

        /// <summary>Chunks the last visible frame with work rebuilt; for tests and the visual pilot.</summary>
        public int LastRebuiltChunks { get; private set; }

        public bool IsVisible => _root != null && _root.activeSelf;

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                if (_root != null) _root.SetActive(false);
                return;
            }
            if (!SubSystems.TryGet(out AreaSubSystem areas)) return;
            if (_cache != areas.FloorVisualCache)
            {
                if (_cache != null) _cache.OnDirty -= MarkDirty;
                _cache = areas.FloorVisualCache;
                _cache.OnDirty += MarkDirty;
                _dirty = true;
            }
            if (_root == null)
            {
                Shader shader = Resources.Load<Shader>("MapEditor/AreaOverlay");
                if (shader == null) return;
                _material = new Material(shader) { name = _name, hideFlags = HideFlags.DontSave };
                if (_renderQueue >= 0) _material.renderQueue = _renderQueue;
                _root = new GameObject(_name) { hideFlags = HideFlags.DontSave };
                _dirty = true;
            }
            _root.SetActive(true);
            if (_dirty)
            {
                Rebuild();
                _dirty = false;
                _dirtyChunks.Clear();
                return;
            }

            if (_tint != null && _dirtyChunks.Count > 0)
                RebuildTintChunks();
        }

        private void RebuildTintChunks()
        {
            foreach (Vector2Int chunk in _dirtyChunks)
            {
                if (!_chunkFilters.TryGetValue(chunk, out MeshFilter filter) || filter == null) continue;
                Mesh old = filter.sharedMesh;
                Mesh mesh = BuildTintChunkMesh(_chunkIds[chunk], _tint, _builder);
                filter.sharedMesh = mesh;
                _meshes.Remove(old);
                if (old != null) UnityEngine.Object.Destroy(old);
                _meshes.Add(mesh);
            }

            LastRebuiltChunks = _dirtyChunks.Count;
            _dirtyChunks.Clear();
        }

        private void MarkDirty() => _dirty = true;

        private void Rebuild()
        {
            foreach (Transform child in _root.transform) child.gameObject.Dispose(true);
            foreach (Mesh mesh in _meshes) UnityEngine.Object.Destroy(mesh);
            _meshes.Clear();
            _chunkFilters.Clear();
            _chunkIds.Clear();
            _chunksOfArea.Clear();
            LabelPositions.Clear();
            Dictionary<ushort, List<Vector3>> tiles = new();
            int built = 0;
            foreach (KeyValuePair<Vector2Int, ushort[]> chunk in _cache.EnumerateChunks())
            {
                for (int i = 0; i < chunk.Value.Length; i++)
                {
                    ushort id = chunk.Value[i];
                    if (id == AreaId.None) continue;
                    if (!tiles.TryGetValue(id, out List<Vector3> positions)) tiles[id] = positions = new();
                    positions.Add(new Vector3(chunk.Key.x * TileConstants.ChunkSize + i % TileConstants.ChunkSize,
                        0.3f, chunk.Key.y * TileConstants.ChunkSize + i / TileConstants.ChunkSize));
                    if (_tint == null) continue;
                    if (!_chunksOfArea.TryGetValue(id, out HashSet<Vector2Int> chunks)) _chunksOfArea[id] = chunks = new();
                    chunks.Add(chunk.Key);
                }
                Mesh mesh = _tint != null ? BuildTintChunkMesh(chunk.Value, _tint, _builder) : BuildChunkMesh(chunk.Value);
                built++;
                // A tint chunk keeps its renderer even when nothing in it shows yet, so a recolour can fill it in place.
                bool hasRooms = _tint != null && Array.Exists(chunk.Value, id => id != AreaId.None);
                if (mesh.vertexCount == 0 && !hasRooms) { UnityEngine.Object.Destroy(mesh); continue; }
                _meshes.Add(mesh);
                GameObject go = new($"Areas {chunk.Key}");
                go.transform.SetParent(_root.transform, false);
                go.transform.localPosition = new Vector3(chunk.Key.x * TileConstants.ChunkSize, 0.22f, chunk.Key.y * TileConstants.ChunkSize);
                MeshFilter meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;
                if (_tint != null)
                {
                    _chunkFilters[chunk.Key] = meshFilter;
                    _chunkIds[chunk.Key] = chunk.Value;
                }
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            foreach (KeyValuePair<ushort, List<Vector3>> area in tiles)
                LabelPositions[area.Key] = ChooseLabelPosition(area.Value);
            LastRebuiltChunks = built;
        }

        public static Vector3 ChooseLabelPosition(IReadOnlyList<Vector3> tiles)
        {
            if (tiles.Count == 0) return Vector3.zero;
            Vector3 center = Vector3.zero;
            foreach (Vector3 tile in tiles) center += tile;
            center /= tiles.Count;
            Vector3 closest = tiles[0];
            foreach (Vector3 tile in tiles)
                if ((tile - center).sqrMagnitude < (closest - center).sqrMagnitude) closest = tile;
            return closest;
        }

        public static Mesh BuildChunkMesh(ushort[] ids)
        {
            List<Vector3> vertices = new();
            List<Color> colors = new();
            List<int> triangles = new();
            if (ids != null)
            {
                int count = Math.Min(ids.Length, TileConstants.ChunkSize * TileConstants.ChunkSize);
                for (int i = 0; i < count; i++)
                {
                    if (ids[i] == AreaId.None) continue;
                    float x = i % TileConstants.ChunkSize;
                    float z = i / TileConstants.ChunkSize;
                    int start = vertices.Count;
                    vertices.Add(new Vector3(x - 0.45f, 0, z - 0.45f));
                    vertices.Add(new Vector3(x + 0.45f, 0, z - 0.45f));
                    vertices.Add(new Vector3(x - 0.45f, 0, z + 0.45f));
                    vertices.Add(new Vector3(x + 0.45f, 0, z + 0.45f));
                    Color tint = AreaDevSettings.GetColorForArea(ids[i]);
                    for (int v = 0; v < 4; v++) colors.Add(tint);
                    triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                    triangles.Add(start + 2); triangles.Add(start + 3); triangles.Add(start + 1);
                }
            }
            Mesh result = new() { name = "AreaOverlayChunk", indexFormat = IndexFormat.UInt32 };
            result.SetVertices(vertices);
            result.SetColors(colors);
            result.SetTriangles(triangles, 0);
            result.RecalculateBounds();
            return result;
        }

        /// <summary>
        /// One chunk of a tinting overlay, in chunk-local space: a flat fill per tile in its room's colour, or a faint fill
        /// under diagonal stripes. Stripes run along the lines where z − x is a whole or half number, so neighbouring
        /// tiles' stripes meet. Colours are stored linear (<see cref="SystemsViewMeshBuilder"/>), as the runs above them.
        /// </summary>
        public static Mesh BuildTintChunkMesh(ushort[] ids, Func<ushort, Tint> tint, SystemsViewMeshBuilder builder = null)
        {
            builder ??= new SystemsViewMeshBuilder();
            builder.Clear();
            Dictionary<ushort, Tint> tints = new();
            int count = ids != null ? Math.Min(ids.Length, TileConstants.ChunkSize * TileConstants.ChunkSize) : 0;
            for (int i = 0; i < count; i++)
            {
                ushort id = ids[i];
                if (id == AreaId.None) continue;
                if (!tints.TryGetValue(id, out Tint room)) tints[id] = room = tint(id);
                if (!room.Show) continue;

                Vector3 centre = new(i % TileConstants.ChunkSize, 0f, i / TileConstants.ChunkSize);
                Color color = room.Color;
                if (!room.Hatched)
                {
                    color.a *= TintAlpha;
                    builder.AddSquare(centre, 0.5f, color);
                    continue;
                }

                Color fill = color;
                fill.a *= HatchFillAlpha;
                builder.AddSquare(centre, 0.5f, fill);
                builder.AddBand(centre + new Vector3(-0.5f, 0f, -0.5f), centre + new Vector3(0.5f, 0f, 0.5f), HatchWidth, color);
                builder.AddBand(centre + new Vector3(-0.5f, 0f, 0f), centre + new Vector3(0f, 0f, 0.5f), HatchWidth, color);
                builder.AddBand(centre + new Vector3(0f, 0f, -0.5f), centre + new Vector3(0.5f, 0f, 0f), HatchWidth, color);
            }

            return builder.ToMesh("AreaTintChunk");
        }

        public void Dispose()
        {
            ClearPaintPreview();
            if (_paintPreview != null) _paintPreview.Dispose(true);
            if (_cache != null) _cache.OnDirty -= MarkDirty;
            _cache = null;
            foreach (Mesh mesh in _meshes) UnityEngine.Object.Destroy(mesh);
            _meshes.Clear();
            if (_root != null) _root.Dispose(true);
            if (_material != null) UnityEngine.Object.Destroy(_material);
        }
    }
}
