#if UNITY_EDITOR
using FishNet.Object;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEditor;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Atmospherics.Editor
{
    /// <summary>
    /// Builds painted copies of <c>AtmosPipesSurface</c>: same line, same connections, only the colour
    /// differs, so players can tell sections of a complex setup apart. The grey pipe stays the
    /// Standard variant of the <c>SurfacePipeVariants/</c> family. Tier B recipe — no MenuItem.
    /// </summary>
    public static class SurfacePipePaintSetup
    {
        private const string PipeFolder = "Assets/Content/WorldObjects/Structures/Pipes/Atmospherics/";
        private const string SurfacePipePrefab = PipeFolder + "AtmosPipesSurface.prefab";
        private const string PaintFolder = PipeFolder + "SurfacePaint/";
        private const string TileFolder = "Assets/Content/Data/TileMap/Resources/Structures/Pipes/Atmospherics/";
        private const string FamilyFolder = TileFolder + "SurfacePipeVariants/";
        private const string BaseTileName = "AtmosphericsPipesSurface.asset";
        private const float Cell = 1f / 16f;

        // The grey pipe's body and highlight sit in the top row of the 16×16 Palette05, columns 3 and 4.
        // Its fittings use columns 1 and 2 and stay grey.
        private const int BodyColumn = 3;
        private const int HighlightColumn = 4;

        // Palette05 cells as (column, row from the top): body (darker), then highlight.
        private static readonly Paint[] Paints =
        {
            new Paint("Red", 4, 3, 5, 3),
            new Paint("Orange", 3, 2, 4, 2),
            new Paint("Yellow", 0, 15, 2, 11),
            new Paint("Green", 10, 8, 9, 8),
            new Paint("Cyan", 4, 8, 5, 8),
            new Paint("Blue", 6, 7, 5, 7),
            new Paint("Purple", 1, 7, 2, 7),
        };

        public static void Run()
        {
            EnsureFolder(PaintFolder);
            EnsureFolder(FamilyFolder);
            MoveBaseTileIntoFamily();

            var baseTile = DB.LoadAssetAtPath<TileObjectSo>(FamilyFolder + BaseTileName);
            var basePrefab = DB.LoadAssetAtPath<GameObject>(SurfacePipePrefab);
            Shapes baseShapes = ReadShapes(basePrefab);

            foreach (Paint paint in Paints)
            {
                string name = "AtmosPipesSurface" + paint.Name;
                Shapes shapes = PaintShapes(baseShapes, paint, PaintFolder + name + "Meshes.asset");
                string path = PaintFolder + name + ".prefab";
                BuildPrefab(name, path, shapes);

                TileObjectSo tile = AtmosInlineDeviceSetup.RegisterTileObject(name, path, TileLayer.PipeSurface, FamilyFolder);
                tile.genericType = baseTile.genericType;
                tile.specificType = baseTile.specificType;
                tile.width = baseTile.width;
                tile.height = baseTile.height;
                tile.placementYOffset = baseTile.placementYOffset;
                EditorUtility.SetDirty(tile);
            }

            DB.SaveAssets();
        }

        private static void MoveBaseTileIntoFamily()
        {
            if (DB.LoadMainAssetAtPath(FamilyFolder + BaseTileName) != null)
                return;

            string error = DB.MoveAsset(TileFolder + BaseTileName, FamilyFolder + BaseTileName);
            if (!string.IsNullOrEmpty(error))
                throw new System.InvalidOperationException(error);
        }

        private static Shapes ReadShapes(GameObject prefab)
        {
            var connector = new SerializedObject(prefab.GetComponent<SimpleAdjacencyConnector>());
            SerializedProperty shapes = connector.FindProperty("simpleAdjacency");
            return new Shapes
            {
                o = (Mesh)shapes.FindPropertyRelative("o").objectReferenceValue,
                u = (Mesh)shapes.FindPropertyRelative("u").objectReferenceValue,
                i = (Mesh)shapes.FindPropertyRelative("i").objectReferenceValue,
                l = (Mesh)shapes.FindPropertyRelative("l").objectReferenceValue,
                t = (Mesh)shapes.FindPropertyRelative("t").objectReferenceValue,
                x = (Mesh)shapes.FindPropertyRelative("x").objectReferenceValue,
            };
        }

        // One mesh asset per colour holds every shape; o and i share a mesh like the grey pipe.
        private static Shapes PaintShapes(Shapes source, Paint paint, string assetPath)
        {
            Mesh i = PaintMesh(source.i, paint, assetPath);
            return new Shapes
            {
                o = source.o == source.i ? i : PaintMesh(source.o, paint, assetPath),
                u = PaintMesh(source.u, paint, assetPath),
                i = i,
                l = PaintMesh(source.l, paint, assetPath),
                t = PaintMesh(source.t, paint, assetPath),
                x = PaintMesh(source.x, paint, assetPath),
            };
        }

        // Rerunning updates the meshes in place, so the prefabs keep their references.
        private static Mesh PaintMesh(Mesh source, Paint paint, string assetPath)
        {
            string meshName = source.name + paint.Name;
            Mesh mesh = FindMesh(assetPath, meshName);
            if (mesh == null)
            {
                mesh = Object.Instantiate(source);
                mesh.name = meshName;
                if (DB.LoadMainAssetAtPath(assetPath) == null)
                    DB.CreateAsset(mesh, assetPath);
                else
                    DB.AddObjectToAsset(mesh, assetPath);
            }
            else
            {
                EditorUtility.CopySerialized(source, mesh);
                mesh.name = meshName;
            }

            Vector2[] uvs = mesh.uv;
            for (int v = 0; v < uvs.Length; v++)
            {
                int column = Mathf.FloorToInt(uvs[v].x / Cell);
                if (column == BodyColumn)
                    uvs[v] = MoveToCell(uvs[v], BodyColumn, paint.BodyColumn, paint.BodyRow);
                else if (column == HighlightColumn)
                    uvs[v] = MoveToCell(uvs[v], HighlightColumn, paint.HighlightColumn, paint.HighlightRow);
            }

            mesh.uv = uvs;
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // Keeps the vertex's offset inside its cell, so the shading inside the cell carries over.
        private static Vector2 MoveToCell(Vector2 uv, int fromColumn, int toColumn, int toRowFromTop) =>
            new Vector2(uv.x + (toColumn - fromColumn) * Cell, uv.y - toRowFromTop * Cell);

        private static Mesh FindMesh(string assetPath, string meshName)
        {
            foreach (Object asset in DB.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Mesh mesh && mesh.name == meshName)
                    return mesh;
            }

            return null;
        }

        private static void BuildPrefab(string name, string path, Shapes shapes)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SurfacePipePrefab);
            try
            {
                root.name = name;
                root.GetComponent<MeshFilter>().sharedMesh = shapes.i;

                var connector = new SerializedObject(root.GetComponent<SimpleAdjacencyConnector>());
                SerializedProperty property = connector.FindProperty("simpleAdjacency");
                property.FindPropertyRelative("o").objectReferenceValue = shapes.o;
                property.FindPropertyRelative("u").objectReferenceValue = shapes.u;
                property.FindPropertyRelative("i").objectReferenceValue = shapes.i;
                property.FindPropertyRelative("l").objectReferenceValue = shapes.l;
                property.FindPropertyRelative("t").objectReferenceValue = shapes.t;
                property.FindPropertyRelative("x").objectReferenceValue = shapes.x;
                connector.ApplyModifiedPropertiesWithoutUndo();

                FishNet.Managing.Object.ManagedObjects.InitializePrefab(root.GetComponent<NetworkObject>(), -1);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureFolder(string folder)
        {
            folder = folder.TrimEnd('/');
            if (DB.IsValidFolder(folder))
                return;

            int slash = folder.LastIndexOf('/');
            DB.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }

        private sealed class Shapes
        {
            public Mesh o;
            public Mesh u;
            public Mesh i;
            public Mesh l;
            public Mesh t;
            public Mesh x;
        }

        private readonly struct Paint
        {
            public readonly string Name;
            public readonly int BodyColumn;
            public readonly int BodyRow;
            public readonly int HighlightColumn;
            public readonly int HighlightRow;

            public Paint(string name, int bodyColumn, int bodyRow, int highlightColumn, int highlightRow)
            {
                Name = name;
                BodyColumn = bodyColumn;
                BodyRow = bodyRow;
                HighlightColumn = highlightColumn;
                HighlightRow = highlightRow;
            }
        }
    }
}
#endif
