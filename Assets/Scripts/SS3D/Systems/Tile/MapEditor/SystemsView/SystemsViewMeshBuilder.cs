using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// Flat vertex-coloured shapes on the overlay's ground plane, for the systems view chunk meshes. Shapes draw in the
    /// order they are added (the overlay shader blends without depth), so add what goes underneath first. Colours are
    /// given as authored (sRGB, as the legend's swatches show them) and stored linear, because the overlay shader
    /// outputs vertex colour unconverted in the project's linear colour space: stored as authored, the palette washes
    /// out to pastel on screen.
    /// </summary>
    public sealed class SystemsViewMeshBuilder
    {
        private readonly List<Vector3> _vertices = new();
        private readonly List<Color> _colors = new();
        private readonly List<int> _triangles = new();

        public int VertexCount => _vertices.Count;

        public void Clear()
        {
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            color = ToStored(color);
            int start = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _triangles.Add(start);
            _triangles.Add(start + 1);
            _triangles.Add(start + 2);
        }

        /// <summary>A quad from four corners in order around its edge.</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            color = ToStored(color);
            int start = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _vertices.Add(d);
            for (int i = 0; i < 4; i++)
                _colors.Add(color);
            _triangles.Add(start);
            _triangles.Add(start + 1);
            _triangles.Add(start + 2);
            _triangles.Add(start);
            _triangles.Add(start + 2);
            _triangles.Add(start + 3);
        }

        /// <summary>An axis-aligned square on the ground plane.</summary>
        public void AddSquare(Vector3 centre, float halfSize, Color color) =>
            AddRect(centre, halfSize, halfSize, color);

        public void AddRect(Vector3 centre, float halfX, float halfZ, Color color) => AddQuad(
            centre + new Vector3(-halfX, 0f, -halfZ),
            centre + new Vector3(-halfX, 0f, halfZ),
            centre + new Vector3(halfX, 0f, halfZ),
            centre + new Vector3(halfX, 0f, -halfZ),
            color);

        /// <summary>A flat band of <paramref name="width"/> from one point to another.</summary>
        public void AddBand(Vector3 from, Vector3 to, float width, Color color)
        {
            Vector3 along = to - from;
            along.y = 0f;
            if (along.sqrMagnitude < 1e-8f)
                return;

            Vector3 side = new Vector3(-along.z, 0f, along.x).normalized * (width * 0.5f);
            AddQuad(from - side, from + side, to + side, to - side, color);
        }

        /// <summary>How a colour is stored in the mesh: linear RGB, alpha unchanged.</summary>
        public static Color ToStored(Color color) =>
            QualitySettings.activeColorSpace == ColorSpace.Linear ? new Color(color.linear.r, color.linear.g, color.linear.b, color.a) : color;

        public Mesh ToMesh(string name)
        {
            Mesh mesh = new() { name = name, indexFormat = _vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(_vertices);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
