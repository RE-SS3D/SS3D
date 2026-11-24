#if UNITY_EDITOR
using System.Collections.Generic;
using FishNet.Object;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Examine;
using SS3D.Systems.Furniture;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.StructuralDamage.Editor
{
    /// <summary>
    /// Builds every single-tile airlock skin in <c>SS13Airlocks.fbx</c> (14 departments, solid and glass) as a copy of
    /// <c>CivillianAirlock</c>: only the leaf meshes, the glass material, the department access fallback and the
    /// examine text differ. Glass skins don't block vision (<see cref="TileObjectSo.blocksVision"/>). Each skin is
    /// its own Map Editor slot: a <c>&lt;Skin&gt;AirlockVariants/</c> family whose Standard and Glass variants cycle with V.
    /// The hand-built Civilian and Security solid doors are left as they are. Rerunning rebuilds the copies in place,
    /// so a change to the Civilian door reaches every skin. Tier B recipe, no MenuItem:
    /// <c>-executeMethod SS3D.Systems.StructuralDamage.Editor.AirlockSkinSetup.RunBatch</c> or call <see cref="Run"/>.
    /// The double-width meshes are not built: the tile system has no two-tile objects yet.
    /// </summary>
    public static class AirlockSkinSetup
    {
        public const string DoorFolder = "Assets/Content/WorldObjects/Structures/Doors/";
        public const string TileFolder = "Assets/Content/Data/TileMap/Resources/Structures/Doors/";
        public const string BasePrefab = DoorFolder + "CivillianAirlock.prefab";
        private const string Fbx = "Assets/Art/Models/Structures/Doors/SS13Airlocks.fbx";
        private const string GlassMaterial = "Assets/Content/WorldObjects/World/Materials/PaletteGlass.mat";
        private const string ExamineFolder = "Assets/Content/Data/Examine/String/Structures/Doors/";
        private const string ItemsDatabase = "Assets/Content/Data/Databases/Items.asset";
        private const string ItemsDatabaseGuid = "c2644d7b34e956743a0bc485e7ca5e37";
        private const string ReferenceFolder = "Assets/Content/Data/ObjectAssetReferences/";

        /// <summary>One department skin: its mesh name in the FBX, the access it falls back to and its examine words.</summary>
        public readonly struct Skin
        {
            public readonly string Mesh;
            public readonly string Label;
            public readonly AccessLevel Access;
            public readonly string Staff;

            public Skin(string mesh, string label, AccessLevel access, string staff)
            {
                Mesh = mesh;
                Label = label;
                Access = access;
                Staff = staff;
            }

            /// <summary>
            /// Prefab and asset-reference name, in the existing <c>CivillianAirlock</c> style. The glass name only adds
            /// <c>Glass</c>, so the palette reads the pair's variants as Standard and Glass.
            /// </summary>
            public string AssetName(bool glass) => Label + "Airlock" + (glass ? "Glass" : "");

            /// <summary>The skin's palette family: <c>EngineeringAirlockVariants/</c> reads "Engineering Airlock".</summary>
            public string FamilyFolder => TileFolder + Label + "AirlockVariants/";

            /// <summary>Tile asset name, like the existing <c>AirlockCivilian</c>.</summary>
            public string TileName(bool glass) => "Airlock" + Mesh + (glass ? "Glass" : "");

            /// <summary>FBX mesh for one leaf: <c>Airlock</c> + skin + <c>Glass</c> + side.</summary>
            public string LeafMesh(bool glass, char side) => "Airlock" + Mesh + (glass ? "Glass" : "") + side;
        }

        /// <summary>Every skin in the FBX. Civilian and Security solid doors already exist and are not rebuilt.</summary>
        public static readonly Skin[] Skins =
        {
            new Skin("", "", AccessLevel.None, null),
            new Skin("Civilian", "Civilian", AccessLevel.Civilian, null),
            new Skin("Maintenance", "Maintenance", AccessLevel.Maintenance, "maintenance access"),
            new Skin("Command", "Command", AccessLevel.Command, "command staff"),
            new Skin("Security", "Security", AccessLevel.Security, "security staff"),
            new Skin("Engineering", "Engineering", AccessLevel.Engineering, "engineering staff"),
            new Skin("Atmospherics", "Atmospherics", AccessLevel.Engineering, "atmospheric technicians"),
            new Skin("Medical", "Medical", AccessLevel.Medical, "medical staff"),
            new Skin("Chemistry", "Chemistry", AccessLevel.Medical, "chemists"),
            new Skin("Genetics", "Genetics", AccessLevel.Medical, "geneticists"),
            new Skin("Virology", "Virology", AccessLevel.Medical, "virologists"),
            new Skin("Surgery", "Surgery", AccessLevel.Medical, "surgeons"),
            new Skin("Science", "Science", AccessLevel.Science, "science staff"),
            new Skin("Supply", "Supply", AccessLevel.Cargo, "cargo staff"),
        };

        /// <summary>The prefab name of each skin, solid and glass; the hand-built Civilian door keeps its spelling.</summary>
        public static string PrefabName(Skin skin, bool glass) =>
            skin.Mesh == "Civilian" ? "CivillianAirlock" + (glass ? "Glass" : "") : skin.AssetName(glass);

        public static bool IsHandBuilt(Skin skin, bool glass) => !glass && (skin.Mesh == "Civilian" || skin.Mesh == "Security");

        public static void Run()
        {
            foreach (Skin skin in Skins)
            {
                EnsureFolder(skin.FamilyFolder);
                if (IsHandBuilt(skin, false))
                    MoveTileTo(skin.TileName(false), skin.FamilyFolder);
            }

            TileObjectSo baseTile = DB.LoadAssetAtPath<TileObjectSo>(Skins[1].FamilyFolder + Skins[1].TileName(false) + ".asset");
            Dictionary<string, Mesh> meshes = LoadFbxMeshes();
            Material glassMaterial = DB.LoadAssetAtPath<Material>(GlassMaterial);

            foreach (Skin skin in Skins)
            {
                foreach (bool glass in new bool[] { false, true })
                {
                    if (IsHandBuilt(skin, glass))
                        continue;

                    string name = PrefabName(skin, glass);
                    string path = DoorFolder + name + ".prefab";
                    ExamineData examine = WireExamine(skin, glass);
                    Mesh left = LeafMesh(meshes, skin, glass, 'L', name);
                    Mesh right = LeafMesh(meshes, skin, glass, 'R', name);
                    BuildPrefab(name, path, left, right, glass ? glassMaterial : null, skin.Access, examine);
                    RegisterTile(skin, glass, name, path, baseTile);
                }
            }

            // The rebuilt prefabs lose their sight boxes; glass skins must stay without (systems/fov.md Sight blockers).
            SS3D.Systems.Tile.Editor.VisionOccluderRecipe.SetupAll();
            DB.SaveAssets();
        }

        public static void RunBatch()
        {
            Run();
            Debug.Log("[AirlockSkinSetup] done");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        // SS13Airlocks.blend gives the Atmospherics glass door's lights the Glass slot and its window the body slot,
        // so the FBX mesh has no light submesh. Same geometry as Engineering glass: copy that door's slots by face.
        private static Mesh LeafMesh(Dictionary<string, Mesh> meshes, Skin skin, bool glass, char side, string prefabName)
        {
            Mesh mesh = meshes[skin.LeafMesh(glass, side)];
            if (!glass || mesh.subMeshCount == 3)
                return mesh;

            Mesh reference = meshes["AirlockEngineeringGlass" + side];
            return RebuildSlots(mesh, reference, DoorFolder + prefabName + "Meshes.asset");
        }

        // Rerunning updates the mesh in place, so the prefab keeps its reference.
        private static Mesh RebuildSlots(Mesh source, Mesh reference, string assetPath)
        {
            Dictionary<Vector3Int, int> slotByFace = new Dictionary<Vector3Int, int>();
            Vector3[] referenceVertices = reference.vertices;
            for (int sub = 0; sub < reference.subMeshCount; sub++)
            {
                int[] triangles = reference.GetTriangles(sub);
                for (int t = 0; t < triangles.Length; t += 3)
                    slotByFace[FaceKey(referenceVertices, triangles, t)] = sub;
            }

            Vector3[] vertices = source.vertices;
            List<int>[] slots = { new List<int>(), new List<int>(), new List<int>() };
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                int[] triangles = source.GetTriangles(sub);
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    if (!slotByFace.TryGetValue(FaceKey(vertices, triangles, t), out int slot))
                        throw new System.InvalidOperationException($"{source.name}: a face has no match in {reference.name}.");

                    slots[slot].Add(triangles[t]);
                    slots[slot].Add(triangles[t + 1]);
                    slots[slot].Add(triangles[t + 2]);
                }
            }

            string meshName = source.name;
            Mesh mesh = null;
            foreach (Object asset in DB.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Mesh existing && existing.name == meshName)
                    mesh = existing;
            }

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

            mesh.subMeshCount = slots.Length;
            for (int slot = 0; slot < slots.Length; slot++)
                mesh.SetTriangles(slots[slot], slot);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // A face's centroid in millimetres; the two doors share geometry, only their UVs differ.
        private static Vector3Int FaceKey(Vector3[] vertices, int[] triangles, int t)
        {
            Vector3 centroid = (vertices[triangles[t]] + vertices[triangles[t + 1]] + vertices[triangles[t + 2]]) / 3f;
            return Vector3Int.RoundToInt(centroid * 1000f);
        }

        private static void BuildPrefab(string name, string path, Mesh left, Mesh right, Material glass, AccessLevel access, ExamineData examine)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BasePrefab);
            try
            {
                root.name = name;
                SetLeaf(root.transform.Find("LeftDoor"), left, glass);
                SetLeaf(root.transform.Find("RightDoor"), right, glass);
                SetLightSlot(root, glass == null ? 1 : 2);

                SerializedObject gate = new SerializedObject(root.GetComponent<AirLockAccessGate>());
                gate.FindProperty("_requireAccess").boolValue = true;
                gate.FindProperty("_fallbackRequiredAccessBits").ulongValue = (ulong)access;
                gate.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject examinable = new SerializedObject(root.GetComponent<StructuralIntegrityExaminable>());
                examinable.FindProperty("key").objectReferenceValue = examine;
                examinable.ApplyModifiedPropertiesWithoutUndo();

                FishNet.Managing.Object.ManagedObjects.InitializePrefab(root.GetComponent<NetworkObject>(), -1);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // The FBX orders a solid leaf's submeshes body, light and a glass leaf's body, glass, light (like the windows),
        // so the light slot moves to 2 on glass doors and the opener and power visual follow it.
        private static void SetLeaf(Transform leaf, Mesh mesh, Material glass)
        {
            leaf.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = leaf.GetComponent<MeshRenderer>();
            Material[] current = renderer.sharedMaterials;
            renderer.sharedMaterials = glass == null
                ? new Material[] { current[0], current[1] }
                : new Material[] { current[0], glass, current[1] };
        }

        private static void SetLightSlot(GameObject root, int slot)
        {
            SerializedObject opener = new SerializedObject(root.GetComponent<AirLockOpener>());
            opener.FindProperty("_doorLightMaterialIndex").intValue = slot;
            opener.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject visual = new SerializedObject(root.GetComponent<SS3D.Systems.Electricity.ConsumerPowerVisual>());
            SerializedProperty indicators = visual.FindProperty("_panelIndicators");
            for (int i = 0; i < indicators.arraySize; i++)
                indicators.GetArrayElementAtIndex(i).FindPropertyRelative("MaterialIndex").intValue = slot;
            visual.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterTile(Skin skin, bool glass, string assetName, string prefabPath, TileObjectSo baseTile)
        {
            ObjectAssetReference reference = Ensure<ObjectAssetReference>(ReferenceFolder + assetName + ".asset");
            reference.Id = DB.AssetPathToGUID(prefabPath);
            reference.Database = ItemsDatabaseGuid;
            EditorUtility.SetDirty(reference);

            SS3D.Data.AssetDatabases.AssetDatabase database = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>(ItemsDatabase);
            AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(reference.Id, database.AssetGroup).address = prefabPath;
            database.Assets[reference.Id] = DB.LoadAssetAtPath<GameObject>(prefabPath);
            EditorUtility.SetDirty(database);

            // Everything but the prefab comes from the Civilian tile, so health, layer and type match.
            string tileName = skin.TileName(glass);
            MoveTileTo(tileName, skin.FamilyFolder);
            TileObjectSo tile = Ensure<TileObjectSo>(skin.FamilyFolder + tileName + ".asset");
            EditorUtility.CopySerialized(baseTile, tile);
            tile.name = tileName;
            tile.PrefabAsset = reference;
            tile.blocksVision = !glass;
            EditorUtility.SetDirty(tile);
        }

        private static ExamineData WireExamine(Skin skin, bool glass)
        {
            string title = (skin.Label.Length > 0 ? skin.Label + " " : "") + (glass ? "Glass Airlock" : "Airlock");
            string description = "A station airlock for " + skin.Staff + ".";
            if (skin.Staff == null)
                description = skin.Mesh == "Civilian" ? "A public station airlock, open to all crew." : "A plain station airlock with no department markings.";
            if (glass)
                description += " Its window lets you see through.";

            string assetName = PrefabName(skin, glass);
            string key = "structures.doors." + Snake(assetName);
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("Examine");
            StringTable table = (StringTable)collection.GetTable(new LocaleIdentifier("en"));
            if (table.GetEntry(key + ".name") is { } nameEntry) nameEntry.Value = title; else table.AddEntry(key + ".name", title);
            if (table.GetEntry(key + ".desc") is { } descEntry) descEntry.Value = description; else table.AddEntry(key + ".desc", description);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);

            ExamineData baseData = DB.LoadAssetAtPath<ExamineData>(ExamineFolder + "CivilianAirlock.asset");
            ExamineData data = Ensure<ExamineData>(ExamineFolder + assetName + ".asset");
            EditorUtility.CopySerialized(baseData, data);
            data.name = assetName;
            data.Name = new LocalizedString("Examine", key + ".name");
            data.Description = new LocalizedString("Examine", key + ".desc");
            EditorUtility.SetDirty(data);
            return data;
        }

        // "MaintenanceAirlockGlass" → "maintenance_airlock_glass".
        private static readonly System.Text.RegularExpressions.Regex WordStart = new System.Text.RegularExpressions.Regex("(?<=[a-z])(?=[A-Z])");

        private static string Snake(string name) => WordStart.Replace(name, "_").ToLowerInvariant();

        private static Dictionary<string, Mesh> LoadFbxMeshes()
        {
            Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
            foreach (Object asset in DB.LoadAllAssetsAtPath(Fbx))
            {
                if (asset is Mesh mesh)
                    meshes[mesh.name] = mesh;
            }

            return meshes;
        }

        // Finds a door tile wherever an earlier layout left it and moves it (GUID kept) into its family folder.
        private static void MoveTileTo(string tileName, string folder)
        {
            string target = folder + tileName + ".asset";
            if (DB.LoadMainAssetAtPath(target) != null)
                return;

            foreach (string guid in DB.FindAssets(tileName + " t:TileObjectSo", new string[] { TileFolder.TrimEnd('/') }))
            {
                string path = DB.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) != tileName)
                    continue;

                string error = DB.MoveAsset(path, target);
                if (!string.IsNullOrEmpty(error))
                    throw new System.InvalidOperationException(error);
                return;
            }
        }

        private static T Ensure<T>(string path) where T : ScriptableObject
        {
            T asset = DB.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            DB.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            folder = folder.TrimEnd('/');
            if (DB.IsValidFolder(folder))
                return;

            int slash = folder.LastIndexOf('/');
            DB.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }
    }
}
#endif
