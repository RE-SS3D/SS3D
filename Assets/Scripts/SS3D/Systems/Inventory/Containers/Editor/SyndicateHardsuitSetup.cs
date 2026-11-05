#if UNITY_EDITOR
using System;
using System.Linq;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Combat;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Roles;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>Repeatable import adaptation and content recipe for the Drive hardsuit.</summary>
    public static class SyndicateHardsuitSetup
    {
        public const string HumanPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
        private const string Art = "Assets/Art/Models/Items/Clothing/Syndicate/";
        private const string Clothing = "Assets/Content/WorldObjects/Items/Clothing/";

        public static int SetupAll()
        {
            var model = DB.LoadAssetAtPath<GameObject>(Art + "SyndicateHardsuit.fbx");
            if (model == null) throw new InvalidOperationException("Missing Syndicate hardsuit FBX");
            var human = DB.LoadAssetAtPath<GameObject>(HumanPath);
            var target = human.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "HumanTorso");
            var palette = DB.LoadAssetAtPath<Material>("Assets/Content/WorldObjects/World/Materials/Palette.mat");
            const string emissionPath = Art + "SyndicateEmission.mat";
            var emission = DB.LoadAssetAtPath<Material>(emissionPath);
            if (emission == null)
            {
                emission = new Material(palette);
                DB.CreateAsset(emission, emissionPath);
            }
            emission.SetTexture("_EmissionMap", palette.GetTexture("_MainTex"));
            emission.SetColor("_EmissionColor", Color.white);
            EditorUtility.SetDirty(emission);
            var materials = new[] { palette, emission };
            var trait = EnsureAsset<Trait>("Assets/Content/Data/Traits/Wearables/ExoSuit.asset");
            var filter = EnsureAsset<Filter>("Assets/Content/Data/Filters/Wearables/ExoSuit.asset");
            var filterSo = new SerializedObject(filter);
            var accepted = filterSo.FindProperty("acceptedTraits");
            accepted.arraySize = 1;
            accepted.GetArrayElementAtIndex(0).objectReferenceValue = trait;
            filterSo.FindProperty("deniedTraits").arraySize = 0;
            filterSo.ApplyModifiedPropertiesWithoutUndo();
            var suit = Build(model, target, "SyndicateHardsuit", "SyndicateHardsuitItem", "JumpsuitGrey", ClothType.ExoSuit, trait, materials);
            var helmet = Build(model, target, "SyndicateHardsuitHelmet", null, "TruckerCap", ClothType.Head,
                DB.LoadAssetAtPath<Trait>("Assets/Content/Data/Traits/Wearables/Hat.asset"), materials);
            var loadout = DB.LoadAssetAtPath<RoleLoadout>("Assets/Content/Data/Roles/Loadouts/OperativeLoadout.asset");
            loadout.EquipmentAssets[ContainerType.ExoSuit] = suit;
            loadout.EquipmentAssets[ContainerType.Head] = helmet;
            EditorUtility.SetDirty(loadout);
            DB.SaveAssets();
            return 2;
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            var asset = DB.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            DB.CreateAsset(asset, path);
            return asset;
        }

        private static ObjectAssetReference Build(GameObject model, SkinnedMeshRenderer target, string name,
            string foldedName, string template, ClothType type, Trait trait, Material[] materials)
        {
            var source = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == name);
            var adapted = AdaptMesh(source, target);
            string meshPath = Art + name + "Worn.asset";
            var mesh = DB.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh = adapted; DB.CreateAsset(mesh, meshPath); }
            else { EditorUtility.CopySerialized(adapted, mesh); UnityEngine.Object.DestroyImmediate(adapted); }
            string path = Clothing + name + ".prefab";
            if (DB.LoadAssetAtPath<GameObject>(path) == null)
                DB.CopyAsset(Clothing + template + ".prefab", path);
            var reference = EnsureAsset<ObjectAssetReference>("Assets/Content/Data/ObjectAssetReferences/" + name + ".asset");
            reference.Id = DB.AssetPathToGUID(path);
            reference.Database = "c2644d7b34e956743a0bc485e7ca5e37";
            EditorUtility.SetDirty(reference);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = name;
                // Retain root physics/network/item; replace the template's visual and attachment children.
                foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                foreach (var collider in root.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
                var worn = new GameObject("WornShape");
                worn.transform.SetParent(root.transform, false);
                var baked = new Mesh(); source.BakeMesh(baked);
                baked.name = name + "Preview";
                // BakeMesh is in source local coordinates (FBX units); adapt the display into metres.
                var previewVertices = baked.vertices;
                for (int i = 0; i < previewVertices.Length; i++) previewVertices[i] = model.transform.worldToLocalMatrix.MultiplyPoint3x4(source.transform.TransformPoint(previewVertices[i]));
                baked.vertices = previewVertices; baked.RecalculateBounds(); baked.RecalculateNormals();
                string previewPath = Art + name + "Preview.asset";
                var preview = DB.LoadAssetAtPath<Mesh>(previewPath);
                if (preview == null) { preview = baked; DB.CreateAsset(preview, previewPath); }
                else { EditorUtility.CopySerialized(baked, preview); UnityEngine.Object.DestroyImmediate(baked); }
                worn.AddComponent<MeshFilter>().sharedMesh = preview;
                worn.AddComponent<MeshRenderer>().sharedMaterials = materials;
                var world = new GameObject("WorldShape"); world.transform.SetParent(root.transform, false);
                Mesh worldMesh;
                if (foldedName != null)
                {
                    var folded = model.GetComponentsInChildren<MeshFilter>(true).First(f => f.name == foldedName);
                    worldMesh = folded.sharedMesh;
                    world.transform.localScale = folded.transform.lossyScale;
                    world.transform.localRotation = folded.transform.rotation;
                }
                else
                {
                    var helm = DB.LoadAssetAtPath<GameObject>(Art + "HardsuitHelmet.fbx").GetComponentInChildren<MeshFilter>();
                    worldMesh = helm.sharedMesh;
                    world.transform.localScale = helm.transform.lossyScale;
                    world.transform.localRotation = helm.transform.rotation;
                }
                world.AddComponent<MeshFilter>().sharedMesh = worldMesh;
                var renderer = world.AddComponent<MeshRenderer>(); renderer.sharedMaterials = materials;
                world.transform.position -= renderer.bounds.center - root.transform.position;
                var box = root.AddComponent<BoxCollider>();
                box.center = Vector3.zero; box.size = renderer.bounds.size;
                var attachment = new GameObject("Attachment"); attachment.transform.SetParent(root.transform, false);
                var item = root.GetComponent<Item>();
                var itemSo = new SerializedObject(item);
                itemSo.FindProperty("_name").stringValue = type == ClothType.Head ? "Syndicate Hardsuit Helmet" : "Syndicate Hardsuit";
                itemSo.FindProperty("_asset").objectReferenceValue = reference;
                itemSo.FindProperty("_weight").floatValue = type == ClothType.Head ? 2f : 8f;
                var traits = itemSo.FindProperty("_startingTraits"); traits.arraySize = 1; traits.GetArrayElementAtIndex(0).objectReferenceValue = trait;
                itemSo.FindProperty("AttachmentPoint").objectReferenceValue = attachment.transform;
                itemSo.FindProperty("AttachmentPointAlt").objectReferenceValue = attachment.transform;
                itemSo.ApplyModifiedPropertiesWithoutUndo();
                var cloth = root.GetComponent<Cloth>();
                if (cloth == null) cloth = root.AddComponent<Cloth>();
                var so = new SerializedObject(cloth);
                var list = so.FindProperty("_clothMeshes._list"); list.arraySize = 1;
                list.GetArrayElementAtIndex(0).FindPropertyRelative("_key").intValue = (int)type;
                list.GetArrayElementAtIndex(0).FindPropertyRelative("_value").objectReferenceValue = mesh;
                var mats = so.FindProperty("_wornMaterials"); mats.arraySize = materials.Length;
                for (int i = 0; i < materials.Length; i++) mats.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
                var occluded = so.FindProperty("_occludedBodyRenderers");
                string[] hidden = type == ClothType.ExoSuit
                    ? new[] { "HumanTorso", "HumanArmLeft", "HumanArmRight", "HumanLegLeft", "HumanLegRight", "UnderwearTop", "UnderwearBottom", "Jumpsuit" }
                    : Array.Empty<string>();
                occluded.arraySize = hidden.Length;
                for (int i = 0; i < hidden.Length; i++) occluded.GetArrayElementAtIndex(i).stringValue = hidden[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                var presentation = root.GetComponent<ClothingItemPresentation>();
                if (presentation == null) presentation = root.AddComponent<ClothingItemPresentation>();
                so = new SerializedObject(presentation);
                so.FindProperty("_worldRoot").objectReferenceValue = world;
                so.FindProperty("_wornShapedRoot").objectReferenceValue = worn;
                so.ApplyModifiedPropertiesWithoutUndo(); worn.SetActive(false);
                var armor = root.GetComponent<ArmorItemExtension>();
                if (armor == null) armor = root.AddComponent<ArmorItemExtension>();
                so = new SerializedObject(armor);
                so.FindProperty("_profile.BruteAbsorption").floatValue = 8f;
                so.FindProperty("_profile.BurnAbsorption").floatValue = 5f;
                so.FindProperty("_profile.MaxIntegrity").floatValue = type == ClothType.Head ? 80f : 160f;
                so.FindProperty("_profile.CoveredZones").intValue = (int)(type == ClothType.Head ? BodyZoneMask.Head : ArmorProfile.SecurityJumpsuit.CoveredZones);
                so.ApplyModifiedPropertiesWithoutUndo();
                StorageContainerPrefabSetup.RebuildNetworkBehaviours(root.GetComponent<FishNet.Object.NetworkObject>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var db = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Items.asset");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.CreateOrMoveEntry(reference.Id, db.AssetGroup).address = name;
            db.Assets[reference.Id] = DB.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(db);
            return reference;
        }

        private static Mesh AdaptMesh(SkinnedMeshRenderer source, SkinnedMeshRenderer target)
        {
            var mesh = UnityEngine.Object.Instantiate(source.sharedMesh);
            mesh.name = source.name + "Worn";
            var weights = mesh.boneWeights;
            var map = source.bones.Select(b => {
                var bone = b;
                while (bone != null && !target.bones.Any(t => t.name == bone.name)) bone = bone.parent;
                if (bone == null) throw new InvalidOperationException("Unmapped bone " + b.name);
                return Array.FindIndex(target.bones, t => t.name == bone.name);
            }).ToArray();
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var transforms = source.bones.Select((b, i) => target.sharedMesh.bindposes[map[i]].inverse * source.sharedMesh.bindposes[i]).ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                var w = weights[i];
                int[] ids = { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 };
                float[] amounts = { w.weight0, w.weight1, w.weight2, w.weight3 };
                Vector3 v = Vector3.zero, n = Vector3.zero;
                for (int j = 0; j < 4; j++) if (amounts[j] > 0f)
                { v += transforms[ids[j]].MultiplyPoint3x4(vertices[i]) * amounts[j]; n += transforms[ids[j]].MultiplyVector(normals[i]) * amounts[j]; }
                vertices[i] = v; normals[i] = n.normalized;
                w.boneIndex0 = map[w.boneIndex0]; w.boneIndex1 = map[w.boneIndex1];
                w.boneIndex2 = map[w.boneIndex2]; w.boneIndex3 = map[w.boneIndex3]; weights[i] = w;
            }
            mesh.vertices = vertices; mesh.normals = normals; mesh.boneWeights = weights;
            mesh.bindposes = target.sharedMesh.bindposes;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
#endif
