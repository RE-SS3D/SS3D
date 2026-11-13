#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Shared steps for building an item prefab from a palette-textured FBX exported by
    /// Tools/export_item_meshes.py: bake the mesh, copy a template prefab that already carries the right
    /// behaviour, swap its visual, and register it (Items database, Addressables, map editor, examine).
    /// Domain recipes (combat, health) own the per-item choices.
    /// </summary>
    public static class ImportedItemPrefab
    {
        private const string ItemsDatabaseId = "c2644d7b34e956743a0bc485e7ca5e37";
        private const string SoundsDatabaseId = "43b69e36c49203a44bf20042cd83de81";
        private const string ReferenceFolder = "Assets/Content/Data/ObjectAssetReferences/";
        private const string PaletteMaterial = "Assets/Content/WorldObjects/World/Materials/Palette.mat";
        private const string EmissionMaterial = "Assets/Art/Models/Items/Weapons/Ranged/Energy/EnergyEmission.mat";

        /// <summary>
        /// Copies one mesh of an FBX into a mesh asset, keeping the FBX's Unity axis conversion. With
        /// <paramref name="centre"/>, the mesh is shifted onto its own bounds centre and <paramref name="offset"/>
        /// says where that centre sat, so a child (a magazine) can be placed back where it was authored.
        /// </summary>
        public static Mesh BakeMesh(string modelPath, string meshName, string assetPath, bool centre,
            out Vector3 offset, out Material[] sourceMaterials)
        {
            GameObject model = DB.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("Missing model: " + modelPath);
            MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
            MeshFilter source = filters.Length == 1 ? filters[0] : filters.Single(f => f.name == meshName);
            Mesh mesh = UnityEngine.Object.Instantiate(source.sharedMesh);
            Matrix4x4 pose = model.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = pose.MultiplyPoint3x4(vertices[i]);
            for (int i = 0; i < normals.Length; i++) normals[i] = pose.inverse.transpose.MultiplyVector(normals[i]).normalized;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            offset = Vector3.zero;
            if (centre)
            {
                offset = mesh.bounds.center;
                for (int i = 0; i < vertices.Length; i++) vertices[i] -= offset;
                mesh.vertices = vertices;
                mesh.RecalculateBounds();
            }

            mesh.normals = normals;
            mesh.name = meshName;
            sourceMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
            Mesh saved = DB.LoadAssetAtPath<Mesh>(assetPath);
            if (saved == null)
            {
                DB.CreateAsset(mesh, assetPath);
                return mesh;
            }

            // CopySerialized can leave the old submesh ranges and bounds behind; rewrite the contents instead.
            saved.Clear();
            saved.indexFormat = mesh.indexFormat;
            saved.vertices = mesh.vertices;
            saved.normals = mesh.normals;
            saved.tangents = mesh.tangents;
            saved.colors32 = mesh.colors32;
            List<Vector4> uvs = new();
            for (int channel = 0; channel < 4; channel++)
            {
                mesh.GetUVs(channel, uvs);
                saved.SetUVs(channel, uvs);
            }

            saved.subMeshCount = mesh.subMeshCount;
            for (int i = 0; i < mesh.subMeshCount; i++) saved.SetTriangles(mesh.GetTriangles(i), i);
            saved.RecalculateBounds();
            saved.name = mesh.name;
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(saved);
            return saved;
        }

        /// <summary>The game's palette material per source slot; the source's emission slot glows.</summary>
        public static Material[] PaletteMaterials(Material[] sourceMaterials)
        {
            Material palette = DB.LoadAssetAtPath<Material>(PaletteMaterial);
            Material emission = DB.LoadAssetAtPath<Material>(EmissionMaterial);
            return sourceMaterials.Select(m => m != null && m.name.Contains("Emission") ? emission : palette).ToArray();
        }

        /// <summary>Copies <paramref name="templatePath"/> to <paramref name="path"/> once and points a reference at it.</summary>
        public static ObjectAssetReference EnsurePrefab(string templatePath, string path, string name)
        {
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            if (DB.LoadAssetAtPath<GameObject>(path) == null && !DB.CopyAsset(templatePath, path))
                throw new InvalidOperationException("Cannot copy " + templatePath + " to " + path);
            ObjectAssetReference reference = EnsureAsset<ObjectAssetReference>(ReferenceFolder + name + ".asset");
            reference.Id = DB.AssetPathToGUID(path);
            reference.Database = ItemsDatabaseId;
            EditorUtility.SetDirty(reference);
            return reference;
        }

        /// <summary>
        /// Drops the template's visual children (everything but <paramref name="keep"/>) and colliders, draws
        /// <paramref name="mesh"/> on the root and fits one box collider around it.
        /// </summary>
        public static void ReplaceVisual(GameObject root, Mesh mesh, Material[] materials, params string[] keep)
        {
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
            {
                if (!keep.Contains(child.name)) UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            foreach (Collider collider in root.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            if (!root.TryGetComponent(out MeshFilter filter)) filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            if (!root.TryGetComponent(out MeshRenderer renderer)) renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size;
        }

        /// <summary>A child that draws its own mesh, placed at <paramref name="localPosition"/> (e.g. a magazine).</summary>
        public static Transform EnsureMeshChild(GameObject root, string name, Mesh mesh, Material[] materials, Vector3 localPosition)
        {
            Transform child = root.transform.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(root.transform, false);
            }

            child.localPosition = localPosition;
            child.localRotation = Quaternion.identity;
            if (!child.TryGetComponent(out MeshFilter filter)) filter = child.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            if (!child.TryGetComponent(out MeshRenderer renderer)) renderer = child.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return child;
        }

        /// <summary>An empty child transform, created when missing.</summary>
        public static Transform EnsureChild(GameObject root, string name)
        {
            Transform child = root.transform.Find(name);
            if (child != null) return child;
            child = new GameObject(name).transform;
            child.SetParent(root.transform, false);
            return child;
        }

        /// <summary>
        /// Item identity and poses. The root is set to the rest rotation, as the item-orientation audit expects.
        /// The icon renders unrotated unless <paramref name="iconRotation"/> is given.
        /// </summary>
        public static void SetItem(GameObject root, ObjectAssetReference reference, string displayName, float weight,
            Transform grip, Vector3 restRotation, bool restsUpright, Vector3? iconRotation)
        {
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(restRotation));
            SerializedObject item = new(root.GetComponent<Item>());
            item.FindProperty("_asset").objectReferenceValue = reference;
            item.FindProperty("_name").stringValue = displayName;
            item.FindProperty("_weight").floatValue = weight;
            item.FindProperty("AttachmentPoint").objectReferenceValue = grip;
            item.FindProperty("AttachmentPointAlt").objectReferenceValue = grip;
            item.FindProperty("_restRotation").vector3Value = restRotation;
            item.FindProperty("_restsUpright").boolValue = restsUpright;
            item.FindProperty("_overrideIconRotation").boolValue = iconRotation.HasValue;
            item.FindProperty("_iconRotation").vector3Value = iconRotation ?? Vector3.zero;
            item.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Items database row, Addressables entry (address = name) and the map editor's item asset.</summary>
        public static void Register(ObjectAssetReference reference, string path, string name, string mapItemFolder)
        {
            SS3D.Data.AssetDatabases.AssetDatabase db =
                DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Items.asset");
            AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(reference.Id, db.AssetGroup).address = name;
            db.Assets[reference.Id] = DB.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(db);
            ItemObjectSo mapItem = EnsureAsset<ItemObjectSo>(mapItemFolder.TrimEnd('/') + "/" + name + ".asset");
            mapItem.PrefabAsset = reference;
            EditorUtility.SetDirty(mapItem);
        }

        /// <summary>
        /// Registers an imported clip in the Sounds database under its file name (forced to mono, as positional
        /// SFX are). Its asset GUID is the id code plays it by.
        /// </summary>
        public static void RegisterSound(string path)
        {
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
                throw new InvalidOperationException("Missing audio clip: " + path);
            if (!importer.forceToMono)
            {
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }

            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            ObjectAssetReference reference = EnsureAsset<ObjectAssetReference>(ReferenceFolder + name + ".asset");
            reference.Id = DB.AssetPathToGUID(path);
            reference.Database = SoundsDatabaseId;
            EditorUtility.SetDirty(reference);
            SS3D.Data.AssetDatabases.AssetDatabase db =
                DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Sounds.asset");
            AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(reference.Id, db.AssetGroup).address = name;
            db.Assets[reference.Id] = DB.LoadAssetAtPath<AudioClip>(path);
            EditorUtility.SetDirty(db);
        }

        /// <summary>English examine name/description under <paramref name="key"/>, wired to the root's examinable.</summary>
        public static void WireExamine(GameObject root, string key, string dataPath, string title, string description)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("Examine");
            if (collection == null || collection.GetTable(new LocaleIdentifier("en")) is not StringTable table)
                throw new InvalidOperationException("Missing Examine English table");
            table.AddEntry(key + ".name", title);
            table.AddEntry(key + ".desc", description);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
            ExamineData data = EnsureAsset<ExamineData>(dataPath);
            data.Name = new LocalizedString("Examine", key + ".name");
            data.Description = new LocalizedString("Examine", key + ".desc");
            EditorUtility.SetDirty(data);
            if (!root.TryGetComponent(out SimpleExaminable examinable)) examinable = root.AddComponent<SimpleExaminable>();
            SerializedObject examine = new(examinable);
            examine.FindProperty("key").objectReferenceValue = data;
            examine.ApplyModifiedPropertiesWithoutUndo();
        }

        public static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            T asset = DB.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            DB.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (DB.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            DB.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
#endif
