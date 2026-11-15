using System;
using System.Collections.Generic;
using Coimbra;
using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// The Atmos view's Room air (creative-mode §4; Documents/architecture/2026-11_map-editor-systems-views.md Q2, Phase 5):
    /// every simulated floor tinted by its pressure (<see cref="AtmosRoomAirPalette"/>), client-local per-chunk meshes in
    /// the <see cref="MapEditorAreaOverlay"/> style under the view's runs. It reads the CPU copy of the atmos snapshot
    /// (<see cref="AtmosRenderContext.Snapshot.PressureData"/>), which the host fills from its simulation and a pure client
    /// from the server's chunk patches, so the editor's <see cref="AtmosRenderContext.IsRenderingSuppressed"/> stays on and
    /// the fog, glow and wind stay hidden. Twice a second it re-reads the snapshot and rebuilds only the chunks where a
    /// tile changed pressure step. Space (vacuum) and walls are not tinted.
    /// </summary>
    public sealed class MapEditorRoomAirOverlay : IDisposable
    {
        /// <summary>Under the Power view's room tint (3999) and the runs (4000).</summary>
        public const int RenderQueue = 3998;
        public const float RefreshSeconds = 0.5f;
        /// <summary>The tint's alpha over the x-ray's dimmed floors.</summary>
        public const float Alpha = 0.35f;
        /// <summary>A tile with nothing to tint.</summary>
        public const byte NotDrawn = byte.MaxValue;
        private const float Height = 0.21f;
        private const int Size = TileConstants.ChunkSize;

        private readonly Dictionary<Vector2Int, Chunk> _chunks = new();
        private readonly HashSet<Vector2Int> _seen = new();
        private readonly List<Vector2Int> _stale = new();
        private readonly SystemsViewMeshBuilder _builder = new();
        private readonly byte[] _scratch = new byte[Size * Size];
        private GameObject _root;
        private Material _material;
        private float _nextRefresh;

        public bool IsVisible => _root != null && _root.activeSelf;

        /// <summary>Tiles the last refresh tinted; for tests and the visual pilot.</summary>
        public int TintedTiles { get; private set; }

        /// <summary>Each tinted tile's pressure step at the last refresh, by tile; for the visual pilot.</summary>
        public bool TryGetLevel(Vector2Int tile, out int level)
        {
            Vector2Int chunk = MapEditorSystemsOverlay.ChunkOf(tile);
            level = NotDrawn;
            if (!_chunks.TryGetValue(chunk, out Chunk drawn))
                return false;

            level = drawn.Levels[(tile.y - chunk.y * Size) * Size + tile.x - chunk.x * Size];
            return level != NotDrawn;
        }

        /// <summary>Every frame while the editor is open; re-reads the snapshot at most every <see cref="RefreshSeconds"/>.</summary>
        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                if (_root != null) _root.SetActive(false);
                _nextRefresh = 0f;
                return;
            }

            if (_root == null)
            {
                Shader shader = Resources.Load<Shader>("MapEditor/AreaOverlay");
                if (shader == null) return;
                _material = new Material(shader) { name = "MapEditorRoomAir", hideFlags = HideFlags.DontSave, renderQueue = RenderQueue };
                _root = new GameObject("MapEditorRoomAir") { hideFlags = HideFlags.DontSave };
            }

            _root.SetActive(true);
            if (Time.unscaledTime < _nextRefresh)
                return;

            _nextRefresh = Time.unscaledTime + RefreshSeconds;
            if (AtmosRenderContext.TryGetSnapshot(out AtmosRenderContext.Snapshot snapshot))
                Refresh(snapshot);
        }

        /// <summary>A tile's step from the snapshot's CPU copy: simulated cells only (not space, walls or empty texels).</summary>
        public static byte LevelAt(in AtmosRenderContext.Snapshot snapshot, Vector2Int tile)
        {
            int width = (int)snapshot.AtlasBounds.z;
            int height = (int)snapshot.AtlasBounds.w;
            int x = tile.x - (int)snapshot.AtlasBounds.x;
            int z = tile.y - (int)snapshot.AtlasBounds.y;
            if (x < 0 || z < 0 || x >= width || z >= height || snapshot.PressureData == null || snapshot.MaskData == null)
                return NotDrawn;

            int texel = z * width + x;
            if (texel >= snapshot.PressureData.Length || texel >= snapshot.MaskData.Length
                || snapshot.MaskData[texel] != AtmosCellVisualEncoding.MaskSimulated)
                return NotDrawn;

            return (byte)AtmosRoomAirPalette.LevelOf(snapshot.PressureData[texel]);
        }

        private void Refresh(in AtmosRenderContext.Snapshot snapshot)
        {
            int minX = (int)snapshot.AtlasBounds.x;
            int minZ = (int)snapshot.AtlasBounds.y;
            Vector2Int from = MapEditorSystemsOverlay.ChunkOf(new Vector2Int(minX, minZ));
            Vector2Int to = MapEditorSystemsOverlay.ChunkOf(new Vector2Int(minX + (int)snapshot.AtlasBounds.z - 1, minZ + (int)snapshot.AtlasBounds.w - 1));
            int tinted = 0;
            _seen.Clear();
            for (int cz = from.y; cz <= to.y; cz++)
            {
                for (int cx = from.x; cx <= to.x; cx++)
                {
                    Vector2Int key = new(cx, cz);
                    bool any = false;
                    for (int i = 0; i < _scratch.Length; i++)
                    {
                        _scratch[i] = LevelAt(snapshot, new Vector2Int(cx * Size + i % Size, cz * Size + i / Size));
                        if (_scratch[i] == NotDrawn) continue;
                        any = true;
                        tinted++;
                    }

                    if (!_chunks.TryGetValue(key, out Chunk chunk))
                    {
                        if (!any) continue;
                        _chunks[key] = chunk = new Chunk(_root.transform, key, _material);
                    }

                    _seen.Add(key);
                    if (chunk.Take(_scratch))
                        chunk.SetMesh(BuildChunkMesh(chunk.Levels, _builder));
                }
            }

            // Chunks the snapshot no longer covers.
            _stale.Clear();
            foreach (Vector2Int key in _chunks.Keys)
            {
                if (!_seen.Contains(key)) _stale.Add(key);
            }

            foreach (Vector2Int key in _stale)
            {
                _chunks[key].Dispose();
                _chunks.Remove(key);
            }

            TintedTiles = tinted;
        }

        /// <summary>One chunk in chunk-local space: a square per tinted tile in its step's colour.</summary>
        public static Mesh BuildChunkMesh(byte[] levels, SystemsViewMeshBuilder builder = null)
        {
            builder ??= new SystemsViewMeshBuilder();
            builder.Clear();
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == NotDrawn) continue;
                Color color = AtmosRoomAirPalette.ColorOfLevel(levels[i]);
                color.a = Alpha;
                builder.AddSquare(new Vector3(i % Size, 0f, i / Size), 0.5f, color);
            }

            return builder.ToMesh("RoomAirChunk");
        }

        public void Dispose()
        {
            foreach (Chunk chunk in _chunks.Values) chunk.Dispose();
            _chunks.Clear();
            if (_root != null) _root.Dispose(true);
            _root = null;
            if (_material != null) UnityEngine.Object.Destroy(_material);
            _material = null;
        }

        private sealed class Chunk : IDisposable
        {
            public readonly byte[] Levels = new byte[Size * Size];
            private readonly GameObject _go;
            private readonly MeshFilter _filter;

            public Chunk(Transform parent, Vector2Int key, Material material)
            {
                Array.Fill(Levels, NotDrawn);
                _go = new GameObject($"Room air {key}");
                _go.transform.SetParent(parent, false);
                _go.transform.localPosition = new Vector3(key.x * Size, Height, key.y * Size);
                _filter = _go.AddComponent<MeshFilter>();
                MeshRenderer renderer = _go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            /// <summary>Copies <paramref name="levels"/> in; true when any tile changed step.</summary>
            public bool Take(byte[] levels)
            {
                bool changed = false;
                for (int i = 0; i < levels.Length; i++)
                {
                    if (Levels[i] == levels[i]) continue;
                    Levels[i] = levels[i];
                    changed = true;
                }

                return changed;
            }

            public void SetMesh(Mesh mesh)
            {
                if (_filter.sharedMesh != null) UnityEngine.Object.Destroy(_filter.sharedMesh);
                _filter.sharedMesh = mesh;
            }

            public void Dispose()
            {
                SetMesh(null);
                _go.Dispose(true);
            }
        }
    }
}
