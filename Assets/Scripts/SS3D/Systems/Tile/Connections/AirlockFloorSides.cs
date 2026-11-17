using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Rules for the floor under an airlock: two half tiles, one per side the door opens onto, each
    /// showing the floor of the cell on that side. A half with no floor to copy is left open, so the
    /// door cell's plenum shows through it.
    /// </summary>
    public static class AirlockFloorSides
    {
        private static readonly Dictionary<GameObject, Material> HalfMaterialByFloorPrefab = new();
        private static readonly Dictionary<(Mesh, int), Mesh> HalfMeshes = new();

        /// <summary>North/south doors use the mesh whose halves lie north and south; east/west doors the other.</summary>
        public static bool UsesNorthSouthMesh(Direction doorDirection) =>
            doorDirection is Direction.North or Direction.South;

        /// <summary>
        /// Submesh of the airlock floor mesh that lies toward <paramref name="side"/>: submesh 0 is the
        /// north (or east) half, submesh 1 the south (or west) half.
        /// </summary>
        public static int SubmeshToward(Direction side) =>
            side is Direction.North or Direction.East ? 0 : 1;

        /// <summary>
        /// The material a half tile copies from a floor prefab, or null when the floor has none worth
        /// copying: no single-material renderer, an adjacency-driven mesh (carpet), or a shared atlas
        /// material such as Palette, whose UVs only make sense on the floor's own mesh.
        /// </summary>
        public static Material GetHalfMaterial(GameObject floorPrefab, IReadOnlyList<Material> atlasMaterials)
        {
            if (floorPrefab == null)
                return null;

            if (HalfMaterialByFloorPrefab.TryGetValue(floorPrefab, out Material cached))
                return cached;

            Material material = ResolveHalfMaterial(floorPrefab, atlasMaterials);
            HalfMaterialByFloorPrefab[floorPrefab] = material;
            return material;
        }

        private static Material ResolveHalfMaterial(GameObject floorPrefab, IReadOnlyList<Material> atlasMaterials)
        {
            if (floorPrefab.GetComponent<IAdjacencyConnector>() != null)
                return null;

            MeshRenderer renderer = floorPrefab.GetComponentInChildren<MeshRenderer>(true);
            if (renderer == null || renderer.sharedMaterials.Length != 1)
                return null;

            Material material = renderer.sharedMaterial;
            if (material == null)
                return null;

            if (atlasMaterials != null)
            {
                for (int i = 0; i < atlasMaterials.Count; i++)
                {
                    if (atlasMaterials[i] == material)
                        return null;
                }
            }

            return material;
        }

        /// <summary>
        /// Re-dress the doors beside a turf that was placed. Plain floors have no adjacency connector,
        /// so the adjacency engine never reaches a neighbouring door for them; <see cref="TileMap"/>
        /// calls this on every turf place and clear instead.
        /// </summary>
        public static void RefreshDoorsBeside(TileMap map, PlacedTileObject turf)
        {
            Vector3 origin = new(turf.WorldOrigin.x, 0f, turf.WorldOrigin.y);
            foreach (Vector2Int gridOffset in turf.GridOffsetList)
                RefreshDoorsBeside(map, origin + new Vector3(gridOffset.x, 0f, gridOffset.y));
        }

        /// <summary>Re-dress the doors on the four cardinal cells around <paramref name="cell"/>.</summary>
        public static void RefreshDoorsBeside(TileMap map, Vector3 cell)
        {
            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction += 2)
            {
                var offset = TileHelper.ToCardinalVector(direction);
                Vector3 neighbour = cell + new Vector3(offset.Item1, 0f, offset.Item2);
                if (map.TryGetTileLocation(TileLayer.Turf, neighbour, out ITileLocation location)
                    && location.TryGetPlacedObject(out PlacedTileObject turf)
                    && turf != null
                    && turf.TryGetComponent(out DoorAdjacencyConnector door))
                {
                    door.RefreshSideFloors(map);
                }
            }
        }

        /// <summary>
        /// One submesh of <paramref name="source"/> as its own mesh, so each half tile can be shown or
        /// hidden alone. Cached per source mesh; the source must be CPU-readable in builds.
        /// </summary>
        public static Mesh GetHalfMesh(Mesh source, int submesh)
        {
            if (source == null || submesh < 0 || submesh >= source.subMeshCount)
                return null;

            if (HalfMeshes.TryGetValue((source, submesh), out Mesh cached) && cached != null)
                return cached;

            Mesh half = new()
            {
                name = $"{source.name}_Half{submesh}",
                hideFlags = HideFlags.DontSave,
            };
            // Keep only the vertices this submesh uses, so the half's bounds (and culling) are its own.
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            Vector4[] tangents = source.tangents;
            Vector2[] uvs = source.uv;
            int[] sourceTriangles = source.GetTriangles(submesh);
            Dictionary<int, int> remap = new();
            List<Vector3> halfVertices = new();
            List<Vector3> halfNormals = new();
            List<Vector4> halfTangents = new();
            List<Vector2> halfUvs = new();
            int[] triangles = new int[sourceTriangles.Length];
            for (int i = 0; i < sourceTriangles.Length; i++)
            {
                int index = sourceTriangles[i];
                if (!remap.TryGetValue(index, out int mapped))
                {
                    mapped = halfVertices.Count;
                    remap[index] = mapped;
                    halfVertices.Add(vertices[index]);
                    if (normals.Length > 0)
                        halfNormals.Add(normals[index]);
                    if (tangents.Length > 0)
                        halfTangents.Add(tangents[index]);
                    if (uvs.Length > 0)
                        halfUvs.Add(uvs[index]);
                }

                triangles[i] = mapped;
            }

            half.SetVertices(halfVertices);
            half.SetNormals(halfNormals);
            half.SetTangents(halfTangents);
            half.SetUVs(0, halfUvs);
            half.SetTriangles(triangles, 0);
            half.RecalculateBounds();
            half.UploadMeshData(markNoLongerReadable: false);

            HalfMeshes[(source, submesh)] = half;
            return half;
        }

        /// <summary>Copies render settings from the source tile renderer onto a half.</summary>
        internal static void CopyRendererSettings(Renderer from, Renderer to)
        {
            to.shadowCastingMode = from.shadowCastingMode;
            to.receiveShadows = from.receiveShadows;
            to.renderingLayerMask = from.renderingLayerMask;
            to.lightProbeUsage = from.lightProbeUsage;
            to.reflectionProbeUsage = from.reflectionProbeUsage;
            to.staticShadowCaster = from.staticShadowCaster;
        }
    }
}
