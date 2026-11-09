#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>Clips a body mesh in its rest pose, retaining skinning and every morph frame.</summary>
    public static class ClothingCoverageMeshBuilder
    {
        private readonly struct VertexSource
        {
            public readonly int A, B;
            public readonly float T;
            public VertexSource(int a, int b, float t) { A = a; B = b; T = t; }
        }

        public static Mesh KeepBelow(Mesh source, float[] heights, float height, Mesh destination = null)
        {
            Vector3[] positions = source.vertices;
            List<VertexSource> vertices = Enumerable.Range(0, positions.Length).Select(i => new VertexSource(i, i, 0f)).ToList();
            List<int[]> submeshes = new();
            Dictionary<(int, int), int> cuts = new();
            int Cut(int a, int b)
            {
                var edge = a < b ? (a, b) : (b, a);
                if (cuts.TryGetValue(edge, out int index)) return index;
                float t = (height - heights[a]) / (heights[b] - heights[a]);
                index = vertices.Count;
                vertices.Add(new VertexSource(a, b, t));
                cuts.Add(edge, index);
                return index;
            }
            for (int submesh = 0; submesh < source.subMeshCount; submesh++)
            {
                int[] triangles = source.GetTriangles(submesh);
                List<int> result = new();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    List<int> polygon = new();
                    for (int j = 0; j < 3; j++)
                    {
                        int a = triangles[i + j], b = triangles[i + (j + 1) % 3];
                        bool insideA = heights[a] <= height, insideB = heights[b] <= height;
                        if (insideA) polygon.Add(a);
                        if (insideA != insideB) polygon.Add(Cut(a, b));
                    }
                    for (int j = 1; j + 1 < polygon.Count; j++)
                    {
                        result.Add(polygon[0]); result.Add(polygon[j]); result.Add(polygon[j + 1]);
                    }
                }
                submeshes.Add(result.ToArray());
            }

            Mesh mesh = destination != null ? destination : new Mesh();
            mesh.ClearBlendShapes();
            mesh.Clear();
            mesh.name = source.name + " - Jumpsuit ankles";
            mesh.indexFormat = source.indexFormat;
            Vector3[] Interpolate(Vector3[] values) => vertices.Select(v => Vector3.LerpUnclamped(values[v.A], values[v.B], v.T)).ToArray();
            mesh.vertices = Interpolate(positions);
            if (source.normals.Length > 0) mesh.normals = Interpolate(source.normals).Select(n => n.normalized).ToArray();
            Vector4[] tangents = source.tangents;
            if (tangents.Length > 0) mesh.tangents = vertices.Select(v => Vector4.LerpUnclamped(tangents[v.A], tangents[v.B], v.T)).ToArray();
            Color[] colors = source.colors;
            if (colors.Length > 0) mesh.colors = vertices.Select(v => Color.LerpUnclamped(colors[v.A], colors[v.B], v.T)).ToArray();
            for (int channel = 0; channel < 8; channel++)
            {
                List<Vector4> uv = new(); source.GetUVs(channel, uv);
                if (uv.Count > 0) mesh.SetUVs(channel, vertices.Select(v => Vector4.LerpUnclamped(uv[v.A], uv[v.B], v.T)).ToList());
            }
            BoneWeight[] weights = source.boneWeights;
            mesh.bindposes = source.bindposes;
            mesh.boneWeights = vertices.Select(v => v.A == v.B ? weights[v.A] : BlendWeights(weights[v.A], weights[v.B], v.T)).ToArray();
            mesh.subMeshCount = source.subMeshCount;
            for (int submesh = 0; submesh < submeshes.Count; submesh++) mesh.SetTriangles(submeshes[submesh], submesh);
            for (int shape = 0; shape < source.blendShapeCount; shape++)
                for (int frame = 0; frame < source.GetBlendShapeFrameCount(shape); frame++)
                {
                    Vector3[] delta = new Vector3[source.vertexCount], normal = new Vector3[source.vertexCount], tangent = new Vector3[source.vertexCount];
                    source.GetBlendShapeFrameVertices(shape, frame, delta, normal, tangent);
                    mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape), source.GetBlendShapeFrameWeight(shape, frame), Interpolate(delta), Interpolate(normal), Interpolate(tangent));
                }
            mesh.RecalculateBounds();
            return mesh;
        }

        private static BoneWeight BlendWeights(BoneWeight a, BoneWeight b, float t)
        {
            Dictionary<int, float> weights = new();
            void Add(int index, float weight) { weights[index] = weights.GetValueOrDefault(index) + weight; }
            Add(a.boneIndex0, a.weight0 * (1f - t)); Add(a.boneIndex1, a.weight1 * (1f - t));
            Add(a.boneIndex2, a.weight2 * (1f - t)); Add(a.boneIndex3, a.weight3 * (1f - t));
            Add(b.boneIndex0, b.weight0 * t); Add(b.boneIndex1, b.weight1 * t);
            Add(b.boneIndex2, b.weight2 * t); Add(b.boneIndex3, b.weight3 * t);
            var sorted = weights.OrderByDescending(p => p.Value).Take(4).ToArray();
            float sum = sorted.Sum(p => p.Value);
            int Index(int i) => i < sorted.Length ? sorted[i].Key : 0;
            float Weight(int i) => i < sorted.Length ? sorted[i].Value / sum : 0f;
            return new BoneWeight { boneIndex0 = Index(0), boneIndex1 = Index(1), boneIndex2 = Index(2), boneIndex3 = Index(3), weight0 = Weight(0), weight1 = Weight(1), weight2 = Weight(2), weight3 = Weight(3) };
        }
    }
}
#endif
