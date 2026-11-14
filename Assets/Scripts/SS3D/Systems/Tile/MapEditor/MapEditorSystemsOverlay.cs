using System;
using System.Collections.Generic;
using Coimbra;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Client-local systems view overlay (Documents/architecture/2026-11_map-editor-systems-views.md Q4), in the
    /// <see cref="MapEditorAreaOverlay"/> style: per chunk, one line mesh for the runs (a dark band under each run hides
    /// the pipe's own paint, the network colour on top) and one mesh of badge quads drawn above the runs. Both use the
    /// area overlay's Resources shader. A delta rebuilds only the chunks it touches; the domain's
    /// <see cref="ISystemsViewStyle"/> places the lines of a tile and draws the badges.
    /// </summary>
    public sealed class MapEditorSystemsOverlay : IDisposable
    {
        /// <summary>
        /// Above floor level; the shader draws on top regardless (ZTest Always), so this plane is where the overlay
        /// sits under the cursor, whatever is in front of it: hover and Select's trace read their tile there.
        /// </summary>
        public const float Height = 0.2f;
        public const float RunWidth = 0.13f;
        public const float UnderlayWidth = 0.22f;
        public static readonly Color Underlay = new(0.04f, 0.05f, 0.07f, 0.8f);
        private const int RunQueue = 4000;
        private const int BadgeQueue = 4001;
        /// <summary>How much of its alpha a network keeps while a trace follows others (creative-mode §4: the rest dims).</summary>
        public const float UntracedAlpha = 0.2f;

        private readonly Dictionary<Vector2Int, Chunk> _chunks = new();
        private readonly HashSet<Vector2Int> _dirty = new();
        private readonly SystemsViewMeshBuilder _builder = new();
        private SystemsViewStructure _structure;
        private ISystemsViewStyle _style;
        private GameObject _root;
        private Material _runMaterial;
        private Material _badgeMaterial;
        private bool _allDirty = true;
        private readonly HashSet<int> _focus = new();

        /// <summary>Chunks rebuilt by the last visible frame that had work; for tests and the visual pilot.</summary>
        public int LastRebuiltChunks { get; private set; }

        public int ChunkCount => _chunks.Count;

        /// <summary>A new structure (or none): every chunk rebuilds on the next visible frame.</summary>
        public void SetStructure(SystemsViewStructure structure)
        {
            _structure = structure;
            _style = structure != null ? SystemsViewStyles.For(structure.View) : null;
            _allDirty = true;
            _dirty.Clear();
        }

        /// <summary>
        /// The networks a trace follows; every other run and badge dims. Empty draws everything at full strength. A change
        /// rebuilds every chunk on the next visible frame.
        /// </summary>
        public void SetFocus(IEnumerable<int> viewIds)
        {
            HashSet<int> next = viewIds != null ? new HashSet<int>(viewIds) : new HashSet<int>();
            if (next.SetEquals(_focus))
                return;

            _focus.Clear();
            _focus.UnionWith(next);
            _allDirty = true;
        }

        /// <summary>The networks the overlay draws at full strength while tracing; empty when no trace is on.</summary>
        public IReadOnlyCollection<int> Focus => _focus;

        /// <summary><paramref name="structure"/> has just applied <paramref name="delta"/>: rebuild only the chunks it touched.</summary>
        public void Apply(SystemsViewStructure structure, SystemsViewDelta delta)
        {
            if (structure != _structure || delta.Reset)
            {
                SetStructure(structure);
                return;
            }

            if (!_allDirty)
                DirtyChunks(delta, _dirty);
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                if (_root != null) _root.SetActive(false);
                return;
            }

            if (_root == null)
            {
                Shader shader = Resources.Load<Shader>("MapEditor/AreaOverlay");
                if (shader == null) return;
                _runMaterial = new Material(shader) { name = "MapEditorSystemsRuns", hideFlags = HideFlags.DontSave, renderQueue = RunQueue };
                _badgeMaterial = new Material(shader) { name = "MapEditorSystemsBadges", hideFlags = HideFlags.DontSave, renderQueue = BadgeQueue };
                _root = new GameObject("MapEditorSystemsOverlay") { hideFlags = HideFlags.DontSave };
                _allDirty = true;
            }

            _root.SetActive(true);
            if (_allDirty)
            {
                foreach (Vector2Int chunk in _chunks.Keys)
                    _dirty.Add(chunk);
                if (_structure != null)
                {
                    foreach (SystemsViewKey key in _structure.Segments.Keys) _dirty.Add(ChunkOf(key.Tile));
                    foreach (SystemsViewKey key in _structure.Badges.Keys) _dirty.Add(ChunkOf(key.Tile));
                }

                _allDirty = false;
            }

            if (_dirty.Count == 0)
                return;

            Rebuild();
        }

        private void Rebuild()
        {
            Dictionary<Vector2Int, List<SystemsViewSegment>> segments = new();
            Dictionary<Vector2Int, List<SystemsViewBadge>> badges = new();
            if (_structure != null && _style != null)
            {
                foreach (SystemsViewSegment segment in _structure.Segments.Values)
                    AddIfDirty(segments, segment.Key.Tile, segment);
                foreach (SystemsViewBadge badge in _structure.Badges.Values)
                    AddIfDirty(badges, badge.Key.Tile, badge);
            }

            foreach (Vector2Int chunk in _dirty)
            {
                segments.TryGetValue(chunk, out List<SystemsViewSegment> chunkSegments);
                badges.TryGetValue(chunk, out List<SystemsViewBadge> chunkBadges);
                if (chunkSegments == null && chunkBadges == null)
                {
                    if (_chunks.Remove(chunk, out Chunk gone)) gone.Dispose();
                    continue;
                }

                if (!_chunks.TryGetValue(chunk, out Chunk target))
                    _chunks[chunk] = target = new Chunk(_root.transform, chunk, _runMaterial, _badgeMaterial);

                target.SetRuns(chunkSegments != null ? BuildRunMesh(chunk, chunkSegments, _style, _builder, _focus) : null);
                target.SetBadges(chunkBadges != null ? BuildBadgeMesh(chunk, chunkBadges, _style, _builder, _focus) : null);
            }

            LastRebuiltChunks = _dirty.Count;
            _dirty.Clear();
        }

        private void AddIfDirty<T>(Dictionary<Vector2Int, List<T>> byChunk, Vector2Int tile, T item)
        {
            Vector2Int chunk = ChunkOf(tile);
            if (!_dirty.Contains(chunk))
                return;

            if (!byChunk.TryGetValue(chunk, out List<T> list))
                byChunk[chunk] = list = new List<T>();
            list.Add(item);
        }

        /// <summary>The chunk a tile falls in (floor division, so negative tiles land in negative chunks).</summary>
        public static Vector2Int ChunkOf(Vector2Int tile) =>
            new(Mathf.FloorToInt(tile.x / (float)TileConstants.ChunkSize), Mathf.FloorToInt(tile.y / (float)TileConstants.ChunkSize));

        /// <summary>Adds the chunks whose meshes <paramref name="delta"/> changes: segments and badges set or removed.</summary>
        public static void DirtyChunks(SystemsViewDelta delta, HashSet<Vector2Int> into)
        {
            if (delta.SegmentsSet != null) foreach (SystemsViewSegment segment in delta.SegmentsSet) into.Add(ChunkOf(segment.Key.Tile));
            if (delta.SegmentsRemoved != null) foreach (SystemsViewKey key in delta.SegmentsRemoved) into.Add(ChunkOf(key.Tile));
            if (delta.BadgesSet != null) foreach (SystemsViewBadge badge in delta.BadgesSet) into.Add(ChunkOf(badge.Key.Tile));
            if (delta.BadgesRemoved != null) foreach (SystemsViewKey key in delta.BadgesRemoved) into.Add(ChunkOf(key.Tile));
        }

        /// <summary>
        /// The runs of one chunk, in chunk-local space: every segment's dark underlay first, then its network colour, each
        /// a joint at the line's spot in the tile plus a half-tile band toward every side it links to. Neighbours draw the
        /// other half, so runs meet at tile edges and chunk borders. A segment with no links draws a short stub.
        /// </summary>
        public static Mesh BuildRunMesh(Vector2Int chunk, IReadOnlyList<SystemsViewSegment> segments, ISystemsViewStyle style,
            SystemsViewMeshBuilder builder = null, ICollection<int> focus = null)
        {
            builder ??= new SystemsViewMeshBuilder();
            builder.Clear();
            Vector2Int origin = chunk * TileConstants.ChunkSize;
            for (int pass = 0; pass < 2; pass++)
            {
                float width = pass == 0 ? UnderlayWidth : RunWidth;
                foreach (SystemsViewSegment segment in segments)
                {
                    Color color = pass == 0 ? Underlay : SystemsViewPalette.ColorFor(segment.ViewId);
                    builder.Alpha = IsFocused(focus, segment.ViewId) ? 1f : UntracedAlpha;
                    Vector2 offset = style?.SlotOffset(segment.Key.Slot) ?? Vector2.zero;
                    float tileX = segment.Key.Tile.x - origin.x;
                    float tileZ = segment.Key.Tile.y - origin.y;
                    Vector3 centre = new(tileX + offset.x, 0f, tileZ + offset.y);
                    builder.AddSquare(centre, width * (segment.Links == 0 ? 1f : 0.5f), color);
                    foreach (Direction side in SystemsViewLinks.Sides)
                    {
                        if (!SystemsViewLinks.Has(segment.Links, side))
                            continue;

                        // To the tile edge along the link, keeping the line's offset across it, so it meets the neighbour's half.
                        Tuple<int, int> step = TileHelper.ToCardinalVector(side);
                        Vector3 edge = new(step.Item1 != 0 ? tileX + step.Item1 * 0.5f : centre.x, 0f,
                            step.Item2 != 0 ? tileZ + step.Item2 * 0.5f : centre.z);
                        builder.AddBand(centre, edge, width, color);
                    }
                }
            }

            return builder.ToMesh("SystemsRunChunk");
        }

        /// <summary>The badges of one chunk, in chunk-local space, as the domain style draws them at each tile centre.</summary>
        public static Mesh BuildBadgeMesh(Vector2Int chunk, IReadOnlyList<SystemsViewBadge> badges, ISystemsViewStyle style,
            SystemsViewMeshBuilder builder = null, ICollection<int> focus = null)
        {
            builder ??= new SystemsViewMeshBuilder();
            builder.Clear();
            Vector2Int origin = chunk * TileConstants.ChunkSize;
            foreach (SystemsViewBadge badge in badges)
            {
                // A device on any traced network stays lit, so a trace shows everything attached to it.
                builder.Alpha = focus == null || focus.Count == 0 || badge.TouchesAny(focus) ? 1f : UntracedAlpha;
                style?.DrawBadge(builder, new Vector3(badge.Key.Tile.x - origin.x, 0f, badge.Key.Tile.y - origin.y), badge);
            }

            return builder.ToMesh("SystemsBadgeChunk");
        }

        /// <summary>Drawn at full strength: no trace is on, or it follows this network.</summary>
        private static bool IsFocused(ICollection<int> focus, int viewId) =>
            focus == null || focus.Count == 0 || (viewId > 0 && focus.Contains(viewId));

        private void DestroyChunks()
        {
            foreach (Chunk chunk in _chunks.Values) chunk.Dispose();
            _chunks.Clear();
        }

        public void Dispose()
        {
            DestroyChunks();
            _structure = null;
            _style = null;
            if (_root != null) _root.Dispose(true);
            _root = null;
            if (_runMaterial != null) UnityEngine.Object.Destroy(_runMaterial);
            if (_badgeMaterial != null) UnityEngine.Object.Destroy(_badgeMaterial);
            _runMaterial = null;
            _badgeMaterial = null;
        }

        /// <summary>One chunk's two renderers; a mesh it no longer needs is destroyed with its object.</summary>
        private sealed class Chunk : IDisposable
        {
            private readonly GameObject _runs;
            private readonly GameObject _badges;

            public Chunk(Transform parent, Vector2Int chunk, Material runMaterial, Material badgeMaterial)
            {
                Vector3 position = new(chunk.x * TileConstants.ChunkSize, Height, chunk.y * TileConstants.ChunkSize);
                _runs = Create(parent, $"Systems runs {chunk}", position, runMaterial);
                _badges = Create(parent, $"Systems badges {chunk}", position, badgeMaterial);
            }

            private static GameObject Create(Transform parent, string name, Vector3 position, Material material)
            {
                GameObject go = new(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = position;
                go.AddComponent<MeshFilter>();
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                go.SetActive(false);
                return go;
            }

            public void SetRuns(Mesh mesh) => Set(_runs, mesh);

            public void SetBadges(Mesh mesh) => Set(_badges, mesh);

            private static void Set(GameObject go, Mesh mesh)
            {
                MeshFilter filter = go.GetComponent<MeshFilter>();
                if (filter.sharedMesh != null) UnityEngine.Object.Destroy(filter.sharedMesh);
                filter.sharedMesh = mesh;
                go.SetActive(mesh != null);
            }

            public void Dispose()
            {
                Set(_runs, null);
                Set(_badges, null);
                _runs.Dispose(true);
                _badges.Dispose(true);
            }
        }
    }
}
