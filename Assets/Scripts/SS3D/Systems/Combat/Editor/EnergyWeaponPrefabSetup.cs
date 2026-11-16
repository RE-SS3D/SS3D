#if UNITY_EDITOR
using System;
using System.Linq;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Combat.Editor
{
    /// <summary>Repeatable energy content import, registered by the combat recipe aggregator.</summary>
    public static class EnergyWeaponPrefabSetup
    {
        private const string Weapons = "Assets/Content/WorldObjects/Items/Weapons/";
        private const string Art = "Assets/Art/Models/Items/Weapons/Ranged/Energy/";
        private const string Sound = "Assets/Art/Sound/Items/Weapons/Energy/SS14/";

        public static int SetupAll()
        {
            RegisterSounds();
            Build("IonRifle", "Ion Rifle", "EnergyRifles", RangedWeaponProfile.IonRifle,
                "A cyan ion rifle. Burns its target; no EMP effect. Use to replenish its charge.");
            Build("LaserRifle", "Laser Rifle", "EnergyRifles", RangedWeaponProfile.LaserRifle,
                "A focused laser rifle. Delivers burn damage. Use to replenish its charge.");
            Build("Taser", "Taser", "EnergyHandguns", RangedWeaponProfile.Taser,
                "A short-range electrical weapon. Knocks an unarmored target down and drains stamina; a hardsuit stops the knockdown. Holds three charges and recharges slowly. Use to replenish its charge.");
            DB.SaveAssets();
            return 3;
        }

        private static void Build(string name, string displayName, string category,
            RangedWeaponProfile profile, string description)
        {
            string modelPath = Art + category + "/" + name + ".fbx";
            GameObject model = DB.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("Missing energy model: " + modelPath);
            MeshFilter source = model.GetComponentsInChildren<MeshFilter>(true).Single();
            // FBX export bakes the authored pose; retain its Unity axis conversion in the copied mesh.
            string meshPath = Art + category + "/" + name + "WeaponMesh.asset";
            Mesh mesh = UnityEngine.Object.Instantiate(source.sharedMesh);
            Matrix4x4 pose = model.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = pose.MultiplyPoint3x4(vertices[i]);
            for (int i = 0; i < normals.Length; i++) normals[i] = pose.inverse.transpose.MultiplyVector(normals[i]).normalized;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.RecalculateBounds();
            mesh.name = name;
            Mesh saved = DB.LoadAssetAtPath<Mesh>(meshPath);
            if (saved == null) DB.CreateAsset(mesh, meshPath);
            else { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); mesh = saved; }

            string path = Weapons + name + ".prefab";
            if (DB.LoadAssetAtPath<GameObject>(path) == null && !DB.CopyAsset(Weapons + "M4.prefab", path))
                throw new InvalidOperationException("Cannot create energy weapon: " + path);
            ObjectAssetReference reference = EnsureAsset<ObjectAssetReference>("Assets/Content/Data/ObjectAssetReferences/" + name + ".asset");
            reference.Id = DB.AssetPathToGUID(path);
            reference.Database = "c2644d7b34e956743a0bc485e7ca5e37";
            EditorUtility.SetDirty(reference);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = name;
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 0f, 90f));
                foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                {
                    if (child.name != "Attachment" && child.name != "Muzzle")
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
                root.GetComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = root.GetComponent<MeshRenderer>();
                Material palette = DB.LoadAssetAtPath<Material>("Assets/Content/WorldObjects/World/Materials/Palette.mat");
                const string emissionPath = Art + "EnergyEmission.mat";
                Material emission = DB.LoadAssetAtPath<Material>(emissionPath);
                if (emission == null)
                {
                    emission = new Material(palette);
                    emission.SetTexture("_EmissionMap", palette.GetTexture("_MainTex"));
                    emission.SetColor("_EmissionColor", Color.white);
                    DB.CreateAsset(emission, emissionPath);
                }
                renderer.sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials
                    .Select(m => m.name.Contains("Emission") ? emission : palette).ToArray();
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                BoxCollider box = root.GetComponent<BoxCollider>();
                box.center = mesh.bounds.center;
                box.size = mesh.bounds.size;
                Transform grip = root.transform.Find("Attachment");
                if (grip == null) grip = new GameObject("Attachment").transform;
                grip.SetParent(root.transform, false);
                grip.localPosition = new Vector3(0f, mesh.bounds.min.y + mesh.bounds.size.y * 0.25f,
                    mesh.bounds.min.z + mesh.bounds.size.z * 0.30f);
                Transform muzzle = root.transform.Find("Muzzle");
                if (muzzle == null) muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(root.transform, false);
                // Blender's authored forward is -Y: after FBX conversion this is +Z.
                muzzle.localPosition = new Vector3(0f, mesh.bounds.max.y - mesh.bounds.size.y * 0.2f, mesh.bounds.max.z);
                muzzle.localRotation = Quaternion.identity;
                // ContainerItemDisplay applies the authored grip directly (it does not invert it).
                // Match the M4's barrel basis in character space despite the different mesh axes.
                GameObject referenceWeapon = DB.LoadAssetAtPath<GameObject>(Weapons + "M4.prefab");
                Transform referenceGrip = referenceWeapon.GetComponent<Item>().AttachmentPoint;
                Transform referenceMuzzle = referenceWeapon.GetComponent<RangedWeaponItemExtension>().Muzzle;
                grip.localRotation = referenceGrip.localRotation * referenceMuzzle.localRotation
                    * Quaternion.Inverse(muzzle.localRotation);
                SerializedObject item = new(root.GetComponent<Item>());
                item.FindProperty("_asset").objectReferenceValue = reference;
                item.FindProperty("_name").stringValue = displayName;
                item.FindProperty("_weight").floatValue = name == "Taser" ? 0.6f : 2.5f;
                item.FindProperty("AttachmentPoint").objectReferenceValue = grip;
                item.FindProperty("AttachmentPointAlt").objectReferenceValue = grip;
                item.ApplyModifiedPropertiesWithoutUndo();
                RangedPrefabSetup.Configure(root, profile);
                WireExamine(root, name, displayName, description);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Register(reference, path, "Items", name);
        }

        private static void RegisterSounds()
        {
            foreach (string guid in DB.FindAssets("t:AudioClip", new string[] { Sound.TrimEnd('/') }))
            {
                string path = DB.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is AudioImporter importer && !importer.forceToMono)
                {
                    importer.forceToMono = true;
                    importer.SaveAndReimport();
                }
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                ObjectAssetReference reference = EnsureAsset<ObjectAssetReference>("Assets/Content/Data/ObjectAssetReferences/" + name + ".asset");
                reference.Id = guid;
                reference.Database = "43b69e36c49203a44bf20042cd83de81";
                EditorUtility.SetDirty(reference);
                Register(reference, path, "Sounds", name);
            }
        }

        private static void Register(ObjectAssetReference reference, string path, string database, string address)
        {
            SS3D.Data.AssetDatabases.AssetDatabase db = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/" + database + ".asset");
            AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(reference.Id, db.AssetGroup).address = address;
            db.Assets[reference.Id] = DB.LoadAssetAtPath<UnityEngine.Object>(path);
            EditorUtility.SetDirty(db);
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            T asset = DB.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); DB.CreateAsset(asset, path); }
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (DB.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            DB.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }

        private static void WireExamine(GameObject root, string name, string title, string description)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("Examine");
            if (collection == null || collection.GetTable(new LocaleIdentifier("en")) is not StringTable table)
                throw new InvalidOperationException("Missing Examine English table");
            string key = "items.weapons." + name.ToLowerInvariant();
            table.AddEntry(key + ".name", title);
            table.AddEntry(key + ".desc", description);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
            ExamineData data = EnsureAsset<ExamineData>("Assets/Content/Data/Examine/String/Items/Weapons/" + name + ".asset");
            data.Name = new LocalizedString("Examine", key + ".name");
            data.Description = new LocalizedString("Examine", key + ".desc");
            EditorUtility.SetDirty(data);
            if (!root.TryGetComponent(out SimpleExaminable examinable)) examinable = root.AddComponent<SimpleExaminable>();
            SerializedObject examine = new(examinable);
            examine.FindProperty("key").objectReferenceValue = data;
            examine.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
