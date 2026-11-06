#if UNITY_EDITOR
using System;
using System.Linq;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.StructuralDamage.Editor
{
    /// <summary>Repeatable explosive content recipe on the structural damage domain aggregator.</summary>
    public static class ExplosivePrefabSetup
    {
        private const string Art = "Assets/Art/Models/Items/Weapons/Explosive/";
        private const string Sound = "Assets/Art/Sound/Items/Weapons/Explosive/SS14/";
        public static void SetupAll()
        {
            RegisterSound("ExplosiveCountdown");
            RegisterSound("MinibombExplosion");
            Build("C4", ExplosiveKind.C4, 0.8f);
            Build("X4", ExplosiveKind.X4, 1.2f);
            Build("SyndicateMiniBomb", ExplosiveKind.Minibomb, 0.5f);
            DB.SaveAssets();
        }

        private static void Build(string name, ExplosiveKind kind, float weight)
        {
            string prefabPath = "Assets/Content/WorldObjects/Items/Weapons/" + name + ".prefab";
            GameObject model = DB.LoadAssetAtPath<GameObject>(Art + name + ".fbx");
            if (model == null) throw new InvalidOperationException("Missing explosive model: " + name);
            MeshFilter source = model.GetComponentsInChildren<MeshFilter>(true).Single();
            Mesh mesh = UnityEngine.Object.Instantiate(source.sharedMesh);
            Matrix4x4 pose = source.transform.localToWorldMatrix;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = pose.MultiplyPoint3x4(vertices[i]);
            for (int i = 0; i < normals.Length; i++) normals[i] = pose.inverse.transpose.MultiplyVector(normals[i]).normalized;
            mesh.vertices = vertices; mesh.normals = normals; mesh.RecalculateBounds();
            Vector3 center = mesh.bounds.center;
            for (int i = 0; i < vertices.Length; i++) vertices[i] -= center;
            mesh.vertices = vertices; mesh.RecalculateBounds(); mesh.name = name;
            Mesh saved = DB.LoadAssetAtPath<Mesh>(Art + name + "Mesh.asset");
            if (saved == null) DB.CreateAsset(mesh, Art + name + "Mesh.asset");
            else { mesh.name = saved.name; EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); mesh = saved; }
            if (DB.LoadAssetAtPath<GameObject>(prefabPath) == null)
                DB.CopyAsset("Assets/Content/WorldObjects/Items/Functional/Tools/Engineering/Wirecutters.prefab", prefabPath);
            ObjectAssetReference reference = Reference(name, prefabPath, "Items", "c2644d7b34e956743a0bc485e7ca5e37");
            string countdown = RegisterSound(kind == ExplosiveKind.Minibomb ? "ExplosiveCountdown" : "ChargeCountdown");
            string defused = RegisterSound("C4Defused");
            Material palette = DB.LoadAssetAtPath<Material>("Assets/Content/WorldObjects/World/Materials/Palette.mat");
            Material lit = Material("C4Led", new Color(3f, 0.12f, 0.025f));
            Material dim = Material("C4LedOff", new Color(0.055f, 0.012f, 0.009f));
            Material black = Material("C4DisplayBacking", new Color(0.008f, 0.01f, 0.012f));
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                root.name = name;
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                    if (child.name != "Attachment" && child.name != "Countdown") UnityEngine.Object.DestroyImmediate(child.gameObject);
                root.GetComponent<MeshFilter>().sharedMesh = mesh;
                root.GetComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(palette, mesh.subMeshCount).ToArray();
                BoxCollider collider = root.GetComponent<BoxCollider>();
                collider.center = mesh.bounds.center; collider.size = mesh.bounds.size;
                Transform grip = root.transform.Find("Attachment");
                grip.localPosition = Vector3.zero; grip.localRotation = Quaternion.identity;
                SerializedObject item = new(root.GetComponent<Item>());
                item.FindProperty("_asset").objectReferenceValue = reference;
                item.FindProperty("_name").stringValue = name == "SyndicateMiniBomb" ? "Syndicate minibomb" : name;
                item.FindProperty("_weight").floatValue = weight;
                item.ApplyModifiedPropertiesWithoutUndo();
                Transform existingScreen = root.transform.Find("Countdown");
                GameObject screen = existingScreen != null ? existingScreen.gameObject : new GameObject("Countdown");
                screen.transform.SetParent(root.transform, false);
                screen.transform.localPosition = new Vector3(0f, mesh.bounds.max.y + 0.002f, 0f);
                Cube(screen.transform, "Backing", Vector3.zero, new Vector3(0.085f, 0.003f, 0.052f), black);
                System.Collections.Generic.List<Renderer> segments = new();
                Vector2[] positions = { new(0,0.017f), new(0.011f,0.0085f), new(0.011f,-0.0085f), new(0,-0.017f), new(-0.011f,-0.0085f), new(-0.011f,0.0085f), new(0,0) };
                for (int digit = 0; digit < 2; digit++)
                    for (int s = 0; s < 7; s++)
                    {
                        bool horizontal = s == 0 || s == 3 || s == 6;
                        Vector3 p = new((digit == 0 ? -0.018f : 0.018f) + positions[s].x, 0.002f, positions[s].y);
                        segments.Add(Cube(screen.transform, "Digit" + digit + "Segment" + s, p,
                            horizontal ? new Vector3(0.02f, 0.001f, 0.003f) : new Vector3(0.003f, 0.001f, 0.014f), dim));
                    }
                if (!screen.TryGetComponent(out ChargeCountdownDisplay display)) display = screen.AddComponent<ChargeCountdownDisplay>();
                SerializedObject d = new(display);
                d.FindProperty("_segments").arraySize = 14;
                for (int i = 0; i < 14; i++) d.FindProperty("_segments").GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
                d.FindProperty("_lit").objectReferenceValue = lit; d.FindProperty("_dim").objectReferenceValue = dim; d.ApplyModifiedPropertiesWithoutUndo();
                if (!root.TryGetComponent(out TimedCharge charge)) charge = root.AddComponent<TimedCharge>();
                SerializedObject c = new(charge);
                c.FindProperty("_kind").enumValueIndex = (int)kind;
                c.FindProperty("_display").objectReferenceValue = kind == ExplosiveKind.Minibomb ? null : display;
                c.FindProperty("_countdownId").stringValue = countdown; c.FindProperty("_confirmId").stringValue = defused;
                c.FindProperty("_wirecuttersAsset").objectReferenceValue = DB.LoadAssetAtPath<GameObject>("Assets/Content/WorldObjects/Items/Functional/Tools/Engineering/Wirecutters.prefab").GetComponent<Item>().Asset;
                c.ApplyModifiedPropertiesWithoutUndo();
                if (kind == ExplosiveKind.Minibomb) UnityEngine.Object.DestroyImmediate(screen);
                WireExamine(root, name, kind);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            GameObject content = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SerializedObject noisy = new(content.GetComponent<Audio.NoisyCollision>());
                noisy.FindProperty("_material").enumValueIndex = (int)(kind == ExplosiveKind.Minibomb ? Audio.ItemCollisionMaterial.Metal : Audio.ItemCollisionMaterial.Plastic);
                noisy.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(content, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(content); }
            DB.SaveAssets();
        }
        private static Renderer Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            Transform existing = parent.Find(name);
            GameObject cube = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name;
            if (cube.TryGetComponent(out Collider collider)) UnityEngine.Object.DestroyImmediate(collider);
            cube.transform.SetParent(parent, false); cube.transform.localPosition = position; cube.transform.localScale = scale;
            Renderer renderer = cube.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return renderer;
        }
        private static Material Material(string name, Color color)
        {
            string path = Art + name + ".mat";
            Material m = DB.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); DB.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color); EditorUtility.SetDirty(m); return m;
        }
        private static string RegisterSound(string name)
        {
            string path = Sound + name + ".ogg";
            AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (!importer.forceToMono) { importer.forceToMono = true; importer.SaveAndReimport(); }
            return Reference(name, path, "Sounds", "43b69e36c49203a44bf20042cd83de81").Id;
        }
        private static ObjectAssetReference Reference(string name, string path, string database, string databaseId)
        {
            ObjectAssetReference r = Ensure<ObjectAssetReference>("Assets/Content/Data/ObjectAssetReferences/" + name + ".asset");
            r.Id = DB.AssetPathToGUID(path); r.Database = databaseId; EditorUtility.SetDirty(r);
            SS3D.Data.AssetDatabases.AssetDatabase db = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/" + database + ".asset");
            AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(r.Id, db.AssetGroup).address = name;
            db.Assets[r.Id] = DB.LoadAssetAtPath<UnityEngine.Object>(path); EditorUtility.SetDirty(db); return r;
        }
        private static T Ensure<T>(string path) where T : ScriptableObject
        {
            T asset = DB.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); DB.CreateAsset(asset, path); } return asset;
        }
        private static void WireExamine(GameObject root, string name, ExplosiveKind kind)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("Examine");
            StringTable table = (StringTable)collection.GetTable(new LocaleIdentifier("en"));
            string key = "items.weapons." + name.ToLowerInvariant();
            string title = kind == ExplosiveKind.Minibomb ? "Syndicate minibomb" : name;
            string description = kind switch
            {
                ExplosiveKind.X4 => "A powerful directional demolition charge. Hold it and use it on a wall or door to attach and arm. Its fifteen-second audible countdown speeds up toward detonation. Extra blast force spreads in a cone through the far side of the target. Wirecutters take three seconds to defuse it.",
                ExplosiveKind.Minibomb => "A compact syndicate grenade. Arm it in hand or use Throw minibomb to pull the pin and throw. Its five-second fuse ends in a small explosion. An armed minibomb cannot be defused.",
                _ => "A timed demolition charge. Place it and arm from the interaction menu, or use it on a wall or door to attach and arm. Its red display counts down fifteen seconds; an audible countdown runs throughout and speeds up at the end. Wirecutters take three seconds to defuse it; stay in reach.",
            };
            table.AddEntry(key + ".name", title);
            table.AddEntry(key + ".desc", description);
            EditorUtility.SetDirty(table); EditorUtility.SetDirty(table.SharedData);
            ExamineData data = Ensure<ExamineData>("Assets/Content/Data/Examine/String/Items/Weapons/" + name + ".asset");
            data.Name = new LocalizedString("Examine", key + ".name"); data.Description = new LocalizedString("Examine", key + ".desc"); EditorUtility.SetDirty(data);
            SerializedObject so = new(root.GetComponent<SimpleExaminable>()); so.FindProperty("key").objectReferenceValue = data; so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
