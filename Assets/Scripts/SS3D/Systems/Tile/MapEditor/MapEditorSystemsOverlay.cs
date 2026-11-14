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
    /// <see cref="MapEditorAreaOverlay"/> style: one mesh per chunk from the structure the server sent, drawn over the
    /// dimmed map with the area overlay's Resources shader. Phase 0 draws each segment as a tile mark in its network
    /// colour; runs as lines, badges and problem markers build on this.
    /// </summary>
    public sealed class MapEditorSystemsOverlay : IDisposable
    {
        /// <summary>Above floor level; the shader draws on top regardless (ZTest Always).</summary>
        private const float Height = 0.2f;
        private const float SegmentHalfSize = 0.2f;
        private const float Alpha = 0.85f;

        private readonly List<Mesh> _meshes = new();
        private SystemsViewStructure _structure;
        private GameObject _root;
        private Material _material;
        private bool _dirty = true;

        /// <summary>The structure to draw; call again after it changes so the next visible frame rebuilds.</summary>
        public void SetStructure(SystemsViewStructure structure)
        {
            _structure = structure;
            _dirty = true;
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
                _material = new Material(shader) { name = "MapEditorSystemsOverlay", hideFlags = HideFlags.DontSave };
                _root = new GameObject("MapEditorSystemsOverlay") { hideFlags = HideFlags.DontSave };
                _dirty = true;
            }

            _root.SetActive(true);
            if (!_dirty) return;
            Rebuild();
            _dirty = false;
        }

        private void Rebuild()
        {
            DestroyChunks();
            if (_structure == null)
                return;

            foreach (KeyValuePair<Vector2Int, List<SystemsViewSegment>> chunk in GroupByChunk(_structure.Segments.Values))
            {
                Mesh mesh = BuildChunkMesh(chunk.Key, chunk.Value);
                _meshes.Add(mesh);
                GameObject go = new($"Systems {chunk.Key}");
                go.transform.SetParent(_root.transform, false);
                go.transform.localPosition = new Vector3(chunk.Key.x * TileConstants.ChunkSize, Height, chunk.Key.y * TileConstants.ChunkSize);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        /// <summary>The chunk a tile falls in (floor division, so negative tiles land in negative chunks).</summary>
        public static Vector2Int ChunkOf(Vector2Int tile) =>
            new(Mathf.FloorToInt(tile.x / (float)TileConstants.ChunkSize), Mathf.FloorToInt(tile.y / (float)TileConstants.ChunkSize));

        public static Dictionary<Vector2Int, List<SystemsViewSegment>> GroupByChunk(IEnumerable<SystemsViewSegment> segments)
        {
            Dictionary<Vector2Int, List<SystemsViewSegment>> chunks = new();
            foreach (SystemsViewSegment segment in segments)
            {
                Vector2Int chunk = ChunkOf(segment.Key.Tile);
                if (!chunks.TryGetValue(chunk, out List<SystemsViewSegment> list))
                    chunks[chunk] = list = new List<SystemsViewSegment>();
                list.Add(segment);
            }

            return chunks;
        }

        /// <summary>One coloured quad per segment, in chunk-local space.</summary>
        public static Mesh BuildChunkMesh(Vector2Int chunk, IReadOnlyList<SystemsViewSegment> segments)
        {
            List<Vector3> vertices = new();
            List<Color> colors = new();
            List<int> triangles = new();
            Vector2Int origin = chunk * TileConstants.ChunkSize;
            foreach (SystemsViewSegment segment in segments)
            {
                float x = segment.Key.Tile.x - origin.x;
                float z = segment.Key.Tile.y - origin.y;
                Color color = SystemsViewPalette.ColorFor(segment.ViewId);
                color.a = Alpha;
                int start = vertices.Count;
                vertices.Add(new Vector3(x - SegmentHalfSize, 0, z - SegmentHalfSize));
                vertices.Add(new Vector3(x + SegmentHalfSize, 0, z - SegmentHalfSize));
                vertices.Add(new Vector3(x - SegmentHalfSize, 0, z + SegmentHalfSize));
                vertices.Add(new Vector3(x + SegmentHalfSize, 0, z + SegmentHalfSize));
                for (int v = 0; v < 4; v++) colors.Add(color);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                triangles.Add(start + 2); triangles.Add(start + 3); triangles.Add(start + 1);
            }

            Mesh mesh = new() { name = "SystemsOverlayChunk", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void DestroyChunks()
        {
            if (_root != null)
            {
                foreach (Transform child in _root.transform) child.gameObject.Dispose(true);
            }

            foreach (Mesh mesh in _meshes) UnityEngine.Object.Destroy(mesh);
            _meshes.Clear();
        }

        public void Dispose()
        {
            DestroyChunks();
            _structure = null;
            if (_root != null) _root.Dispose(true);
            _root = null;
            if (_material != null) UnityEngine.Object.Destroy(_material);
            _material = null;
        }
    }
}
