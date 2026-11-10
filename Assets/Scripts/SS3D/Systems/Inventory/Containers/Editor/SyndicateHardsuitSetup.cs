#if UNITY_EDITOR
using System;
using System.Linq;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Combat;
using SS3D.Systems.Examine;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Roles;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.Localization;
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
            var magboot = BuildMagboot(model, target, materials);
            var loadout = DB.LoadAssetAtPath<RoleLoadout>("Assets/Content/Data/Roles/Loadouts/OperativeLoadout.asset");
            loadout.EquipmentAssets[ContainerType.ExoSuit] = suit;
            loadout.EquipmentAssets[ContainerType.Head] = helmet;
            loadout.EquipmentAssets[ContainerType.ShoeLeft] = magboot;
            EditorUtility.SetDirty(loadout);
            DB.SaveAssets();
            return 3;
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
            var mesh = SaveMesh(AdaptMesh(source, target), Art + name + "Worn.asset");
            var reference = EnsurePrefab(name, template, out string path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = name;
                var preview = SaveMesh(BakePreview(model, source, name + "Preview"), Art + name + "Preview.asset");
                // A helmet does not fold, so it looks the same on the head, in the hand and on the floor.
                var folded = foldedName != null
                    ? model.GetComponentsInChildren<MeshFilter>(true).First(f => f.name == foldedName)
                    : null;
                ReplaceShapes(root, preview, folded != null ? folded.sharedMesh : preview, folded != null ? folded.transform : root.transform,
                    materials, out var worn, out var world, out var attachment);
                // The folded pile lies flat; hold it on edge against the leg rather than as a tray across the body.
                if (folded != null) attachment.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var item = root.GetComponent<Item>();
                var itemSo = new SerializedObject(item);
                itemSo.FindProperty("_name").stringValue = type == ClothType.Head ? "Syndicate Hardsuit Helmet" : "Syndicate Hardsuit";
                itemSo.FindProperty("_asset").objectReferenceValue = reference;
                itemSo.FindProperty("_weight").floatValue = ItemSizeWeightTable.WeightOf("Clothing/" + name);
                itemSo.FindProperty("_sizeClass").enumValueIndex = (int)ItemSizeWeightTable.SizeOf("Clothing/" + name);
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
                WirePresentation(root, world, worn);
                var armor = root.GetComponent<ArmorItemExtension>();
                if (armor == null) armor = root.AddComponent<ArmorItemExtension>();
                so = new SerializedObject(armor);
                so.FindProperty("_profile.BruteAbsorption").floatValue = 8f;
                so.FindProperty("_profile.BurnAbsorption").floatValue = 5f;
                so.FindProperty("_profile.MaxIntegrity").floatValue = type == ClothType.Head ? 80f : 160f;
                so.FindProperty("_profile.CoveredZones").intValue = (int)(type == ClothType.Head ? BodyZoneMask.Head : ArmorProfile.SecurityJumpsuit.CoveredZones);
                so.FindProperty("_profile.BlocksStun").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                if (root.GetComponent<SealedGearItemExtension>() == null) root.AddComponent<SealedGearItemExtension>();
                WireExamine(root, name, type == ClothType.Head ? "syndicate_hardsuit_helmet" : "syndicate_hardsuit");
                StorageContainerPrefabSetup.RebuildNetworkBehaviours(root.GetComponent<FishNet.Object.NetworkObject>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            RegisterItem(reference, path, name);
            return reference;
        }

        /// <summary>One Item is the whole pair, like every shoe; the pair in the FBX becomes a mesh per side.</summary>
        private static ObjectAssetReference BuildMagboot(GameObject model, SkinnedMeshRenderer target, Material[] materials)
        {
            const string name = "SyndicateMagboots";
            var source = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == name);
            bool[] left = source.sharedMesh.boneWeights.Select(w => source.bones[w.boneIndex0].name.EndsWith("_l")).ToArray();
            var adapted = AdaptMesh(source, target);
            var leftMesh = SaveMesh(KeepVertices(adapted, i => left[i], name + "WornLeft"), Art + name + "WornLeft.asset");
            var rightMesh = SaveMesh(KeepVertices(adapted, i => !left[i], name + "WornRight"), Art + name + "WornRight.asset");
            UnityEngine.Object.DestroyImmediate(adapted);

            var reference = EnsurePrefab(name, "ShoeJackboots", out string path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = name;
                // The worn stance puts the boots in line with the icon camera, so both forms use the item pose.
                var item = model.GetComponentsInChildren<MeshFilter>(true).First(f => f.name == name + "Item");
                ReplaceShapes(root, item.sharedMesh, item.sharedMesh, item.transform, materials, out var worn, out var world, out var attachment);
                worn.transform.SetLocalPositionAndRotation(world.transform.localPosition, world.transform.localRotation);
                worn.transform.localScale = world.transform.localScale;

                var itemSo = new SerializedObject(root.GetComponent<Item>());
                itemSo.FindProperty("_name").stringValue = "Syndicate Magboots";
                itemSo.FindProperty("_asset").objectReferenceValue = reference;
                itemSo.FindProperty("_weight").floatValue = ItemSizeWeightTable.WeightOf("Clothing/" + name);
                itemSo.FindProperty("_sizeClass").enumValueIndex = (int)ItemSizeWeightTable.SizeOf("Clothing/" + name);
                itemSo.FindProperty("AttachmentPoint").objectReferenceValue = attachment.transform;
                itemSo.FindProperty("AttachmentPointAlt").objectReferenceValue = attachment.transform;
                itemSo.ApplyModifiedPropertiesWithoutUndo();

                var so = new SerializedObject(root.GetComponent<Cloth>());
                var list = so.FindProperty("_clothMeshes._list");
                list.arraySize = 2;
                list.GetArrayElementAtIndex(0).FindPropertyRelative("_key").intValue = (int)ClothType.ShoeLeft;
                list.GetArrayElementAtIndex(0).FindPropertyRelative("_value").objectReferenceValue = leftMesh;
                list.GetArrayElementAtIndex(1).FindPropertyRelative("_key").intValue = (int)ClothType.ShoeRight;
                list.GetArrayElementAtIndex(1).FindPropertyRelative("_value").objectReferenceValue = rightMesh;
                var mats = so.FindProperty("_wornMaterials");
                mats.arraySize = materials.Length;
                for (int i = 0; i < materials.Length; i++) mats.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                WirePresentation(root, world, worn);
                if (root.GetComponent<MagbootItemExtension>() == null) root.AddComponent<MagbootItemExtension>();
                WireExamine(root, name, "syndicate_magboots");
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            RegisterItem(reference, path, name);
            return reference;
        }

        private static ObjectAssetReference EnsurePrefab(string name, string template, out string path)
        {
            path = Clothing + name + ".prefab";
            if (DB.LoadAssetAtPath<GameObject>(path) == null)
                DB.CopyAsset(Clothing + template + ".prefab", path);
            var reference = EnsureAsset<ObjectAssetReference>("Assets/Content/Data/ObjectAssetReferences/" + name + ".asset");
            reference.Id = DB.AssetPathToGUID(path);
            reference.Database = "c2644d7b34e956743a0bc485e7ca5e37";
            EditorUtility.SetDirty(reference);
            return reference;
        }

        private static void RegisterItem(ObjectAssetReference reference, string path, string name)
        {
            var db = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Items.asset");
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.CreateOrMoveEntry(reference.Id, db.AssetGroup).address = name;
            db.Assets[reference.Id] = DB.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(db);
        }

        /// <summary>Retains root physics/network/item; replaces the template's visual and attachment children.</summary>
        private static void ReplaceShapes(GameObject root, Mesh preview, Mesh worldMesh, Transform worldPose, Material[] materials,
            out GameObject worn, out GameObject world, out GameObject attachment)
        {
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach (var collider in root.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            // Some templates draw on their root, and that mesh would stay on over the new shapes.
            foreach (var renderer in root.GetComponents<MeshRenderer>()) UnityEngine.Object.DestroyImmediate(renderer);
            foreach (var filter in root.GetComponents<MeshFilter>()) UnityEngine.Object.DestroyImmediate(filter);
            worn = new GameObject("WornShape");
            worn.transform.SetParent(root.transform, false);
            worn.AddComponent<MeshFilter>().sharedMesh = preview;
            worn.AddComponent<MeshRenderer>().sharedMaterials = materials;
            world = new GameObject("WorldShape"); world.transform.SetParent(root.transform, false);
            world.transform.localScale = worldPose.lossyScale;
            world.transform.localRotation = worldPose.rotation;
            world.AddComponent<MeshFilter>().sharedMesh = worldMesh;
            var worldRenderer = world.AddComponent<MeshRenderer>(); worldRenderer.sharedMaterials = materials;
            world.transform.position -= worldRenderer.bounds.center - root.transform.position;
            var box = root.AddComponent<BoxCollider>();
            box.center = Vector3.zero; box.size = worldRenderer.bounds.size;
            attachment = new GameObject("Attachment"); attachment.transform.SetParent(root.transform, false);
        }

        /// <summary>The template's key names the template, so each piece gets its own. Text lives in ExamineIdentificationKeySetup.</summary>
        private static void WireExamine(GameObject root, string name, string key)
        {
            var data = EnsureAsset<ExamineData>("Assets/Content/Data/Examine/String/Items/Clothing/" + name + ".asset");
            data.Name = new LocalizedString("Examine", "items.clothing." + key + ".name");
            data.Description = new LocalizedString("Examine", "items.clothing." + key + ".desc");
            EditorUtility.SetDirty(data);
            var examine = new SerializedObject(root.GetComponent<SimpleExaminable>());
            examine.FindProperty("key").objectReferenceValue = data;
            examine.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePresentation(GameObject root, GameObject world, GameObject worn)
        {
            var presentation = root.GetComponent<ClothingItemPresentation>();
            if (presentation == null) presentation = root.AddComponent<ClothingItemPresentation>();
            var so = new SerializedObject(presentation);
            so.FindProperty("_worldRoot").objectReferenceValue = world;
            so.FindProperty("_wornShapedRoot").objectReferenceValue = worn;
            so.ApplyModifiedPropertiesWithoutUndo(); worn.SetActive(false);
        }

        private static Mesh BakePreview(GameObject model, SkinnedMeshRenderer source, string name)
        {
            var baked = new Mesh(); source.BakeMesh(baked);
            baked.name = name;
            // BakeMesh is in source local coordinates (FBX units); adapt the display into metres.
            var previewVertices = baked.vertices;
            for (int i = 0; i < previewVertices.Length; i++) previewVertices[i] = model.transform.worldToLocalMatrix.MultiplyPoint3x4(source.transform.TransformPoint(previewVertices[i]));
            baked.vertices = previewVertices; baked.RecalculateBounds(); baked.RecalculateNormals();
            return baked;
        }

        private static Mesh SaveMesh(Mesh built, string path)
        {
            var mesh = DB.LoadAssetAtPath<Mesh>(path);
            if (mesh == null) { DB.CreateAsset(built, path); return built; }
            EditorUtility.CopySerialized(built, mesh);
            UnityEngine.Object.DestroyImmediate(built);
            return mesh;
        }

        /// <summary>Copies the triangles whose corners all pass, keeping submeshes, skinning and vertex order.</summary>
        private static Mesh KeepVertices(Mesh mesh, Func<int, bool> keep, string name)
        {
            int[] remap = new int[mesh.vertexCount];
            int kept = 0;
            for (int i = 0; i < remap.Length; i++) remap[i] = keep(i) ? kept++ : -1;
            T[] Pick<T>(T[] all) => all.Length == 0 ? all : all.Where((_, i) => remap[i] >= 0).ToArray();

            var part = new Mesh { name = name };
            part.vertices = Pick(mesh.vertices);
            part.normals = Pick(mesh.normals);
            part.tangents = Pick(mesh.tangents);
            part.uv = Pick(mesh.uv);
            part.boneWeights = Pick(mesh.boneWeights);
            part.bindposes = mesh.bindposes;
            part.subMeshCount = mesh.subMeshCount;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                int[] triangles = mesh.GetTriangles(sub);
                var indices = new System.Collections.Generic.List<int>();
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (remap[triangles[i]] >= 0 && remap[triangles[i + 1]] >= 0 && remap[triangles[i + 2]] >= 0)
                        indices.AddRange(new[] { remap[triangles[i]], remap[triangles[i + 1]], remap[triangles[i + 2]] });
                }
                part.SetTriangles(indices, sub);
            }
            part.RecalculateBounds();
            return part;
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
