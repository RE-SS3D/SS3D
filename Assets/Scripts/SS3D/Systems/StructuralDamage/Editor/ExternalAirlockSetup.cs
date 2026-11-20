#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Electricity;
using SS3D.Systems.Examine;
using SS3D.Systems.Furniture;
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
    /// Builds the external airlock, the heavy door between the station and space, from <c>Firelock.fbx</c>
    /// (exported by <c>Tools/export_firelock.py</c>). It is a copy of <c>CivillianAirlock</c> with the two sliding
    /// leaves swapped for the skinned model, which sinks into the floor as it opens. It opens only through the
    /// Open interaction (<c>AirLockOpener._opensOnApproach</c> off) and closes on its own after
    /// <see cref="AutoCloseDelay"/>, once the doorway is clear. The airlock animator is reused through an override
    /// controller whose clips the recipe builds from the FBX's one action. It opens and closes with SS14's external
    /// airlock sounds. Tier B recipe, no MenuItem:
    /// <c>-executeMethod SS3D.Systems.StructuralDamage.Editor.ExternalAirlockSetup.RunBatch</c> or call <see cref="Run"/>.
    /// </summary>
    public static class ExternalAirlockSetup
    {
        public const string Name = "ExternalAirlock";
        public const string PrefabPath = AirlockSkinSetup.DoorFolder + Name + ".prefab";
        public const string TilePath = AirlockSkinSetup.TileFolder + "AirlockExternal.asset";
        public const float AutoCloseDelay = 6f;

        private const string Fbx = "Assets/Art/Models/Structures/Doors/Firelock.fbx";
        private const string ModelChild = "Model";
        private const string BaseController = AirlockSkinSetup.DoorFolder + "AirlockAnimator.controller";
        private const string OpeningClip = AirlockSkinSetup.DoorFolder + Name + "Opening.anim";
        private const string ClosingClip = AirlockSkinSetup.DoorFolder + Name + "Closing.anim";
        private const string Controller = AirlockSkinSetup.DoorFolder + Name + "Animator.overrideController";
        private const string PaletteMaterial = "Assets/Content/WorldObjects/World/Materials/Palette.mat";
        private const string ExaminePath = "Assets/Content/Data/Examine/String/Structures/Doors/" + Name + ".asset";
        private const string ExamineKey = "structures.doors.external_airlock";
        private const string ReferencePath = "Assets/Content/Data/ObjectAssetReferences/" + Name + ".asset";
        private const string ItemsDatabase = "Assets/Content/Data/Databases/Items.asset";
        private const string ItemsDatabaseGuid = "c2644d7b34e956743a0bc485e7ca5e37";
        private const string SoundFolder = "Assets/Art/Sound/World/Airlocks/";
        private const string SoundsDatabaseGuid = "43b69e36c49203a44bf20042cd83de81";

        // The solid door rides the Top bone, the visible top edge of the door the whole way down, so it leaves
        // the doorway as the door sinks. Sized to the closed door in the prefab's space.
        private const string BlockerBone = "FirelockArmature/Bottom2/Bottom/Top";
        private static readonly Vector3 BlockerCenter = new Vector3(0f, 1.0f, 0f);
        private static readonly Vector3 BlockerSize = new Vector3(1.0f, 2.0f, 0.5f);

        public static void Run()
        {
            RemapMaterial();
            AnimationClip source = DB.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Single(clip => !clip.name.StartsWith("__preview"));
            AnimationClip opening = WriteClip(source, OpeningClip, reverse: false);
            AnimationClip closing = WriteClip(source, ClosingClip, reverse: true);
            AnimatorOverrideController controller = WriteController(opening, closing);
            ExamineData examine = WireExamine();
            string openSound = RegisterSound("ExternalAirlockOpen");
            string closeSound = RegisterSound("ExternalAirlockClose");
            BuildPrefab(controller, examine, openSound, closeSound);
            RegisterTile();
            DB.SaveAssets();
        }

        public static void RunBatch()
        {
            Run();
            Debug.Log("[ExternalAirlockSetup] done");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        private static void RemapMaterial()
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(Fbx);
            Material palette = DB.LoadAssetAtPath<Material>(PaletteMaterial);
            AssetImporter.SourceAssetIdentifier id = new AssetImporter.SourceAssetIdentifier(typeof(Material), "Palette05");
            if (importer.GetExternalObjectMap().TryGetValue(id, out Object mapped) && mapped == palette)
                return;

            importer.AddRemap(id, palette);
            importer.SaveAndReimport();
        }

        // The FBX clip animates paths under the model root; the prefab's Animator sits one level up.
        // Closing plays the opening backwards.
        private static AnimationClip WriteClip(AnimationClip source, string path, bool reverse)
        {
            AnimationClip clip = DB.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                DB.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            clip.frameRate = source.frameRate;
            float length = source.length;
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(source, binding);
                if (reverse)
                {
                    Keyframe[] keys = curve.keys.Select(key => new Keyframe(length - key.time, key.value, -key.outTangent, -key.inTangent))
                        .OrderBy(key => key.time).ToArray();
                    curve = new AnimationCurve(keys);
                }

                EditorCurveBinding moved = binding;
                moved.path = string.IsNullOrEmpty(binding.path) ? ModelChild : ModelChild + "/" + binding.path;
                AnimationUtility.SetEditorCurve(clip, moved, curve);
            }

            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorOverrideController WriteController(AnimationClip opening, AnimationClip closing)
        {
            AnimatorOverrideController controller = DB.LoadAssetAtPath<AnimatorOverrideController>(Controller);
            if (controller == null)
            {
                controller = new AnimatorOverrideController(DB.LoadAssetAtPath<RuntimeAnimatorController>(BaseController));
                DB.CreateAsset(controller, Controller);
            }

            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            controller.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip original = overrides[i].Key;
                AnimationClip replacement = original.name.Contains("Closing") ? closing : opening;
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, replacement);
            }

            controller.ApplyOverrides(overrides);
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void BuildPrefab(AnimatorOverrideController controller, ExamineData examine, string openSound, string closeSound)
        {
            if (DB.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                DB.CopyAsset(AirlockSkinSetup.BasePrefab, PrefabPath);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.name = Name;
                foreach (string leaf in new string[] { "LeftDoor", "RightDoor", ModelChild })
                {
                    Transform old = root.transform.Find(leaf);
                    if (old != null)
                        Object.DestroyImmediate(old.gameObject);
                }

                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(DB.LoadAssetAtPath<GameObject>(Fbx), root.transform);
                model.name = ModelChild;
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                SkinnedMeshRenderer skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
                skin.sharedMaterials = new Material[] { DB.LoadAssetAtPath<Material>(PaletteMaterial) };

                GameObject blocker = new GameObject("Blocker");
                blocker.transform.SetParent(root.transform, false);
                blocker.transform.localPosition = BlockerCenter;
                blocker.transform.SetParent(model.transform.Find(BlockerBone), true);
                BoxCollider box = blocker.AddComponent<BoxCollider>();
                box.size = Vector3.Scale(BlockerSize, InverseScale(blocker.transform.lossyScale));

                Animator animator = root.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.avatar = null;

                SerializedObject opener = new SerializedObject(root.GetComponent<AirLockOpener>());
                opener.FindProperty("_opensOnApproach").boolValue = false;
                opener.FindProperty("_autoCloseDelay").floatValue = AutoCloseDelay;
                opener.FindProperty("_openSoundId").stringValue = openSound;
                opener.FindProperty("_closeSoundId").stringValue = closeSound;
                opener.FindProperty("_meshesToColor").ClearArray();
                opener.FindProperty("_skinnedMeshesToColor").ClearArray();
                opener.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject visual = new SerializedObject(root.GetComponent<ConsumerPowerVisual>());
                visual.FindProperty("_panelIndicators").ClearArray();
                visual.ApplyModifiedPropertiesWithoutUndo();

                // Room access decides; an unassigned external airlock is public.
                SerializedObject gate = new SerializedObject(root.GetComponent<AirLockAccessGate>());
                gate.FindProperty("_fallbackRequiredAccessBits").ulongValue = 0;
                gate.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject examinable = new SerializedObject(root.GetComponent<StructuralIntegrityExaminable>());
                examinable.FindProperty("key").objectReferenceValue = examine;
                examinable.ApplyModifiedPropertiesWithoutUndo();

                FishNet.Managing.Object.ManagedObjects.InitializePrefab(root.GetComponent<NetworkObject>(), -1);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // SS14's airlock_ext_open/close (see Assets/Art/Sound/ATTRIBUTIONS.md), in the Sounds database.
        private static string RegisterSound(string name)
        {
            string path = SoundFolder + name + ".ogg";
            AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (!importer.forceToMono)
            {
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }

            return ExplosivePrefabSetup.Reference(name, path, "Sounds", SoundsDatabaseGuid).Id;
        }

        private static Vector3 InverseScale(Vector3 scale) => new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);

        private static ExamineData WireExamine()
        {
            const string title = "External Airlock";
            const string description = "A heavy airlock that seals the station off from space. It opens only when someone works it "
                + "by hand, sinking into the floor, and shuts again on its own once the doorway is clear.";
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("Examine");
            StringTable table = (StringTable)collection.GetTable(new LocaleIdentifier("en"));
            if (table.GetEntry(ExamineKey + ".name") is { } nameEntry) nameEntry.Value = title; else table.AddEntry(ExamineKey + ".name", title);
            if (table.GetEntry(ExamineKey + ".desc") is { } descEntry) descEntry.Value = description; else table.AddEntry(ExamineKey + ".desc", description);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);

            ExamineData baseData = DB.LoadAssetAtPath<ExamineData>("Assets/Content/Data/Examine/String/Structures/Doors/CivilianAirlock.asset");
            ExamineData data = Ensure<ExamineData>(ExaminePath);
            EditorUtility.CopySerialized(baseData, data);
            data.name = Name;
            data.Name = new LocalizedString("Examine", ExamineKey + ".name");
            data.Description = new LocalizedString("Examine", ExamineKey + ".desc");
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void RegisterTile()
        {
            ObjectAssetReference reference = Ensure<ObjectAssetReference>(ReferencePath);
            reference.Id = DB.AssetPathToGUID(PrefabPath);
            reference.Database = ItemsDatabaseGuid;
            EditorUtility.SetDirty(reference);

            SS3D.Data.AssetDatabases.AssetDatabase database = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>(ItemsDatabase);
            AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(reference.Id, database.AssetGroup).address = PrefabPath;
            database.Assets[reference.Id] = DB.LoadAssetAtPath<GameObject>(PrefabPath);
            EditorUtility.SetDirty(database);

            // Health, layer and type follow the Civilian door; solid, so it blocks sight.
            TileObjectSo baseTile = DB.LoadAssetAtPath<TileObjectSo>(AirlockSkinSetup.TileFolder + "CivilianAirlockVariants/AirlockCivilian.asset");
            TileObjectSo tile = Ensure<TileObjectSo>(TilePath);
            EditorUtility.CopySerialized(baseTile, tile);
            tile.name = "AirlockExternal";
            tile.PrefabAsset = reference;
            tile.blocksVision = true;
            EditorUtility.SetDirty(tile);
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
    }
}
#endif
