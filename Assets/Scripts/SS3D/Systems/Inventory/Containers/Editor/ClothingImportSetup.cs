#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Combat;
using SS3D.Systems.Examine;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Roles;
using UnityEditor;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;
using Hardsuit = SS3D.Systems.Inventory.Containers.Editor.SyndicateHardsuitSetup;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Turns the job clothing sets written by <c>Tools/clothing/export_clothing.py</c> into clothing Items.
    /// Each garment is a copy of its slot's existing prefab (so storage, traits and attachment points carry
    /// over) with the worn mesh remapped onto Human's bones by name. Run <see cref="SetupAll"/> after a
    /// re-export; it is repeatable and overwrites the meshes and prefabs it owns. Examine text for these
    /// keys lives in ExamineIdentificationKeySetup.AddIdentificationCardTemplateKeys.
    /// </summary>
    public static class ClothingImportSetup
    {
        public const string Art = "Assets/Art/Models/Items/Clothing/Jobs/";
        private const string Meshes = Art + "Meshes/";
        private const string Clothing = "Assets/Content/WorldObjects/Items/Clothing/";
        private const string Wearables = "Assets/Content/Data/Traits/Wearables/";
        private const string Loadouts = "Assets/Content/Data/Roles/Loadouts/";
        private const string PalettePath = "Assets/Content/WorldObjects/World/Materials/Palette.mat";

        /// <summary>How a garment is worn, which decides its template prefab and cloth slots.</summary>
        public enum Slot
        {
            Jumpsuit,
            /// <summary>Worn over the jumpsuit in the ExoSuit slot: coats and vests. Never sealed.</summary>
            Outer,
            Head,
            Glasses,
            Back,
            Shoes,
            Gloves,
        }

        public sealed class Garment
        {
            public readonly string Name;
            public readonly string Set;
            public readonly Slot Slot;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly ArmorProfile? Armor;

            /// <summary>
            /// Covers torso, arms and legs wrist to ankle with no openings, so the body under it can be hidden.
            /// The Drive jumpsuits need this: they are cut tighter than the in-game body, which pokes through.
            /// </summary>
            public readonly bool CoversBody;

            public Garment(string name, string set, Slot slot, string displayName, string description, ArmorProfile? armor = null,
                bool coversBody = false)
            {
                Name = name;
                Set = set;
                Slot = slot;
                DisplayName = displayName;
                Description = description;
                Armor = armor;
                CoversBody = coversBody;
            }

            /// <summary>Examine key stem, e.g. <c>jumpsuit_captain</c>.</summary>
            public string Key => string.Concat(Name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
        }

        // Armor values are first-pass tuning (armor.md §2: a vest is strong against brute, weak against burn).
        private static ArmorProfile Vest(float brute, float burn, float integrity) => new()
        {
            BruteAbsorption = brute,
            BurnAbsorption = burn,
            MaxIntegrity = integrity,
            CoveredZones = BodyZoneMask.Chest | BodyZoneMask.Groin,
        };

        private static ArmorProfile Helmet(float brute, float burn, float integrity) => new()
        {
            BruteAbsorption = brute,
            BurnAbsorption = burn,
            MaxIntegrity = integrity,
            CoveredZones = BodyZoneMask.Head,
        };

        /// <summary>Every garment the importer owns. Names match the objects in the exported FBX sets.</summary>
        public static readonly IReadOnlyList<Garment> Garments = new Garment[]
        {
            new("JumpsuitCaptain", "Command", Slot.Jumpsuit, "Captain's Jumpsuit", "A navy jumpsuit with gold trim, cut for whoever is in charge.", coversBody: true),
            new("CapCaptain", "Command", Slot.Head, "Captain's Garrison Cap", "A blue garrison cap with a gold badge."),
            new("BackpackCommand", "Command", Slot.Back, "Command Backpack", "A backpack in command blue."),
            new("JumpsuitMedical", "Medical", Slot.Jumpsuit, "Medical Scrubs", "White scrubs, easy to spot blood on.", coversBody: true),
            new("LabCoat", "Medical", Slot.Outer, "Lab Coat", "A long white coat worn over the uniform."),
            new("BackpackMedical", "Medical", Slot.Back, "Medical Backpack", "A white backpack with a green stripe."),
            new("ArmorVestSecurity", "Security", Slot.Outer, "Armor Vest", "A ballistic vest. Good against blows and bullets, poor against fire.", Vest(10f, 3f, 150f)),
            new("HelmetSecurity", "Security", Slot.Head, "Security Helmet", "A hard helmet marked POLICE.", Helmet(8f, 3f, 100f)),
            new("CapSecurity", "Security", Slot.Head, "Security Cap", "A red cap marked POLICE."),
            new("GlassesSecurityHud", "Security", Slot.Glasses, "Security Glasses", "Red-tinted security glasses."),
            new("BackpackSecurity", "Security", Slot.Back, "Security Backpack", "A black backpack with red trim."),
            new("HardHatYellow", "Engineering", Slot.Head, "Hard Hat", "A yellow hard hat with a lamp."),
            new("ShoeWorkBoots", "Engineering", Slot.Shoes, "Work Boots", "A pair of laced yellow work boots."),
            new("WeldingMask", "Engineering", Slot.Head, "Welding Helmet", "A dark-visored welding helmet."),
            new("GogglesMeson", "Engineering", Slot.Glasses, "Meson Goggles", "Green-lensed engineering goggles."),
            new("JumpsuitSyndicate", "Syndicate", Slot.Jumpsuit, "Tactical Turtleneck", "Black fatigues with red bands. Nobody on the crew wears these.", coversBody: true),
            new("ArmorVestSyndicate", "Syndicate", Slot.Outer, "Syndicate Armor Vest", "A black plate vest with shoulder guards.", Vest(12f, 5f, 160f)),
            new("ShoeSyndicateBoots", "Syndicate", Slot.Shoes, "Combat Boots", "A pair of black combat boots."),
            new("GloveSyndicate", "Syndicate", Slot.Gloves, "Combat Gloves", "Black fingerless combat gloves."),
        };

        public static int SetupAll()
        {
            if (!DB.IsValidFolder(Art + "Meshes")) DB.CreateFolder(Art.TrimEnd('/'), "Meshes");
            var human = DB.LoadAssetAtPath<GameObject>(Hardsuit.HumanPath);
            var target = human.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "HumanTorso");
            var palette = DB.LoadAssetAtPath<Material>(PalettePath);
            var models = new Dictionary<string, GameObject>();
            foreach (Garment garment in Garments)
            {
                if (!models.TryGetValue(garment.Set, out GameObject model))
                {
                    model = DB.LoadAssetAtPath<GameObject>(Art + garment.Set + ".fbx");
                    if (model == null) throw new InvalidOperationException($"Missing {Art}{garment.Set}.fbx; run Tools/clothing/export_clothing.py");
                    models[garment.Set] = model;
                }

                if (garment.Slot is Slot.Jumpsuit or Slot.Outer) BuildShaped(model, target, garment, palette);
                else BuildRooted(model, target, garment, palette);
            }

            DB.SaveAssets();
            // The prefabs on disk are right, but replacing their meshes can leave the editor's loaded copies pointing at
            // dead meshes until a restart; reimport them so a rerun followed by Play shows the new clothes.
            foreach (Garment garment in Garments)
                DB.ImportAsset(Clothing + garment.Name + ".prefab", ImportAssetOptions.ForceUpdate);
            return Garments.Count;
        }

        /// <summary>Dresses the job loadouts in their department clothing.</summary>
        public static void ApplyLoadouts()
        {
            Dress("CaptainLoadout", (ContainerType.Jumpsuit, "JumpsuitCaptain"), (ContainerType.Head, "CapCaptain"), (ContainerType.Bag, "BackpackCommand"));
            Dress("DoctorLoadout", (ContainerType.Jumpsuit, "JumpsuitMedical"), (ContainerType.ExoSuit, "LabCoat"), (ContainerType.Bag, "BackpackMedical"));
            Dress("SecurityLoadout", (ContainerType.ExoSuit, "ArmorVestSecurity"), (ContainerType.Head, "HelmetSecurity"),
                (ContainerType.Glasses, "GlassesSecurityHud"), (ContainerType.Bag, "BackpackSecurity"));
            Dress("EngineerLoadout", (ContainerType.Head, "HardHatYellow"), (ContainerType.ShoeLeft, "ShoeWorkBoots"), (ContainerType.Glasses, "GogglesMeson"));
            // Operatives keep the hardsuit, helmet and magboots; the fatigues and gloves go underneath.
            Dress("OperativeLoadout", (ContainerType.Jumpsuit, "JumpsuitSyndicate"), (ContainerType.GloveLeft, "GloveSyndicate"));
            DB.SaveAssets();
        }

        private static void Dress(string loadoutName, params (ContainerType slot, string item)[] pieces)
        {
            var loadout = DB.LoadAssetAtPath<RoleLoadout>(Loadouts + loadoutName + ".asset");
            foreach ((ContainerType slot, string item) in pieces)
            {
                var reference = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.ObjectAssetReference>("Assets/Content/Data/ObjectAssetReferences/" + item + ".asset");
                if (reference == null) throw new InvalidOperationException($"{item} has no asset reference; run SetupAll first");
                loadout.EquipmentAssets[slot] = reference;
            }

            EditorUtility.SetDirty(loadout);
        }

        private static SkinnedMeshRenderer Worn(GameObject model, Garment garment)
        {
            SkinnedMeshRenderer renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == garment.Name);
            if (renderer == null) throw new InvalidOperationException($"{garment.Set}.fbx has no skinned mesh named {garment.Name}");
            return renderer;
        }

        /// <summary>Jumpsuits and outer layers: a folded (or worn-shaped) world form and a worn-shaped child.</summary>
        private static void BuildShaped(GameObject model, SkinnedMeshRenderer target, Garment garment, Material palette)
        {
            SkinnedMeshRenderer source = Worn(model, garment);
            Mesh mesh = Hardsuit.SaveMesh(Hardsuit.AdaptMesh(source, target), Meshes + garment.Name + "Worn.asset");
            var reference = Hardsuit.EnsurePrefab(garment.Name, "JumpsuitGrey", out string path);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = garment.Name;
                Mesh preview = Hardsuit.SaveMesh(Hardsuit.BakePreview(model, source, garment.Name + "Preview"), Meshes + garment.Name + "Preview.asset");
                MeshFilter folded = model.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f => f.name == garment.Name + "Folded");
                Material[] materials = { palette };
                Hardsuit.ReplaceShapes(root, preview, folded != null ? folded.sharedMesh : preview, folded != null ? folded.transform : root.transform,
                    materials, out GameObject worn, out GameObject world, out GameObject attachment);
                // The folded pile lies flat; hold it on edge against the leg rather than as a tray across the body.
                if (folded != null) attachment.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                Trait trait = DB.LoadAssetAtPath<Trait>(Wearables + (garment.Slot == Slot.Jumpsuit ? "Jumpsuit" : "ExoSuit") + ".asset");
                var item = new SerializedObject(root.GetComponent<Item>());
                SetItemFields(item, garment, reference, attachment.transform);
                SerializedProperty traits = item.FindProperty("_startingTraits");
                traits.arraySize = 1;
                traits.GetArrayElementAtIndex(0).objectReferenceValue = trait;
                item.ApplyModifiedPropertiesWithoutUndo();

                ClothType type = garment.Slot == Slot.Jumpsuit ? ClothType.Jumpsuit : ClothType.ExoSuit;
                SetCloth(root, new[] { (type, mesh) }, materials, garment.CoversBody ? CoveredBody : Array.Empty<string>());
                Hardsuit.WirePresentation(root, world, worn);
                Finish(root, garment, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Hardsuit.RegisterItem(reference, path, garment.Name);
        }

        /// <summary>Hats, glasses, backpacks, shoes and gloves: the worn mesh is also the world mesh, as on their templates.</summary>
        private static void BuildRooted(GameObject model, SkinnedMeshRenderer target, Garment garment, Material palette)
        {
            SkinnedMeshRenderer source = Worn(model, garment);
            Mesh adapted = Hardsuit.AdaptMesh(source, target);
            (ClothType type, Mesh mesh)[] meshes;
            bool[] left = source.sharedMesh.boneWeights.Select(w => source.bones[w.boneIndex0].name.EndsWith("_l")).ToArray();
            Func<int, bool> paired = null;
            if (garment.Slot is Slot.Shoes or Slot.Gloves)
            {
                // One Item is the whole pair; split it into a mesh per side by the bone each vertex follows.
                // On the floor it shows one side, as the template shoes and gloves do.
                paired = i => left[i];
                bool shoes = garment.Slot == Slot.Shoes;
                meshes = new[]
                {
                    (shoes ? ClothType.ShoeLeft : ClothType.GloveLeft,
                        Hardsuit.SaveMesh(Hardsuit.KeepVertices(adapted, i => left[i], garment.Name + "WornLeft"), Meshes + garment.Name + "WornLeft.asset")),
                    (shoes ? ClothType.ShoeRight : ClothType.GloveRight,
                        Hardsuit.SaveMesh(Hardsuit.KeepVertices(adapted, i => !left[i], garment.Name + "WornRight"), Meshes + garment.Name + "WornRight.asset")),
                };
                UnityEngine.Object.DestroyImmediate(adapted);
            }
            else
            {
                adapted.name = garment.Name + "Worn";
                meshes = new[] { (SingleType(garment.Slot), Hardsuit.SaveMesh(adapted, Meshes + garment.Name + "Worn.asset")) };
            }

            Mesh world = WorldMesh(model, source, garment, paired);
            var reference = Hardsuit.EnsurePrefab(garment.Name, Template(garment.Slot), out string path);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = garment.Name;
                root.GetComponent<MeshFilter>().sharedMesh = world;
                root.GetComponent<MeshRenderer>().sharedMaterials = new[] { palette };
                if (root.TryGetComponent(out MeshCollider collider)) collider.sharedMesh = world;
                KeepAttachmentsOn(root, world.bounds);

                var item = new SerializedObject(root.GetComponent<Item>());
                SetItemFields(item, garment, reference, null);
                item.ApplyModifiedPropertiesWithoutUndo();
                SetCloth(root, meshes, new[] { palette }, Array.Empty<string>());
                Finish(root, garment, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Hardsuit.RegisterItem(reference, path, garment.Name);
        }

        /// <summary>
        /// The world (floor and hand) mesh of a rooted garment: the worn shape baked Y-up, moved to where the template's
        /// own mesh sits so the template's attachment points still land on it. The worn mesh can't double for it, as it
        /// does on the templates: after the bone remap it is in Human's bind space, Z-up and offset from the item.
        /// </summary>
        private static Mesh WorldMesh(GameObject model, SkinnedMeshRenderer source, Garment garment, Func<int, bool> keep)
        {
            Mesh baked = Hardsuit.BakePreview(model, source, garment.Name + "World");
            if (keep != null)
            {
                Mesh side = Hardsuit.KeepVertices(baked, keep, garment.Name + "World");
                UnityEngine.Object.DestroyImmediate(baked);
                baked = side;
            }

            var template = DB.LoadAssetAtPath<GameObject>(Clothing + Template(garment.Slot) + ".prefab");
            Vector3 shift = template.GetComponent<MeshFilter>().sharedMesh.bounds.center - baked.bounds.center;
            Vector3[] vertices = baked.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] += shift;
            baked.vertices = vertices;
            baked.RecalculateBounds();
            return Hardsuit.SaveMesh(baked, Meshes + garment.Name + "World.asset");
        }

        /// <summary>
        /// Hats and helmets sit upright on their rim. Anything else rests on its broadest side: the candidate
        /// rotation that leaves the item thinnest vertically (ItemRestPoseAudit).
        /// </summary>
        private static void SetRestPose(GameObject root, Garment garment)
        {
            var item = new SerializedObject(root.GetComponent<Item>());
            bool upright = garment.Slot == Slot.Head;
            item.FindProperty("_restsUpright").boolValue = upright;
            if (!upright)
            {
                Vector3[] candidates = { Vector3.zero, new(90f, 0f, 0f), new(0f, 0f, 90f), new(-90f, 0f, 0f) };
                Vector3 best = Vector3.zero;
                float bestRatio = float.MaxValue;
                foreach (Vector3 euler in candidates)
                {
                    root.transform.rotation = Quaternion.Euler(euler);
                    Bounds bounds = default;
                    bool any = false;
                    foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                    {
                        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                        if (any) bounds.Encapsulate(renderer.bounds);
                        else { bounds = renderer.bounds; any = true; }
                    }

                    if (!any) continue;
                    // Keep the template's own pose when it already lies flat; otherwise take the flattest.
                    if (euler == Vector3.zero && ItemRestPoseAudit.LiesFlat(bounds.size)) break;
                    float ratio = bounds.size.y / Mathf.Max(0.001f, Mathf.Min(bounds.size.x, bounds.size.z));
                    if (ratio < bestRatio - 0.01f)
                    {
                        bestRatio = ratio;
                        best = euler;
                    }
                }

                // The rest rotation is the prefab root's rotation (ItemRestPoseRecipe).
                root.transform.rotation = Quaternion.Euler(best);
                item.FindProperty("_restRotation").vector3Value = best;
            }

            item.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>The template's grips sit on the template's mesh; pull any that miss the new mesh onto its bounds.</summary>
        private static void KeepAttachmentsOn(GameObject root, Bounds bounds)
        {
            var item = root.GetComponent<Item>();
            foreach (Transform point in new[] { item.AttachmentPoint, item.AttachmentPointAlt })
            {
                if (point == null || point.parent != root.transform) continue;
                Vector3 inset = bounds.extents * 0.9f;
                Vector3 local = point.localPosition;
                point.localPosition = new Vector3(
                    Mathf.Clamp(local.x, bounds.center.x - inset.x, bounds.center.x + inset.x),
                    Mathf.Clamp(local.y, bounds.center.y - inset.y, bounds.center.y + inset.y),
                    Mathf.Clamp(local.z, bounds.center.z - inset.z, bounds.center.z + inset.z));
            }
        }

        private static ClothType SingleType(Slot slot) => slot switch
        {
            Slot.Head => ClothType.Head,
            Slot.Glasses => ClothType.Glasses,
            Slot.Back => ClothType.Backpack,
            _ => throw new ArgumentOutOfRangeException(nameof(slot)),
        };

        private static string Template(Slot slot) => slot switch
        {
            Slot.Head => "TruckerCap",
            Slot.Glasses => "Sunglasses",
            Slot.Back => "Backpack",
            Slot.Shoes => "ShoeJackboots",
            Slot.Gloves => "GloveInsulated",
            _ => "JumpsuitGrey",
        };

        private static void SetItemFields(SerializedObject item, Garment garment, SS3D.Data.AssetDatabases.ObjectAssetReference reference, Transform attachment)
        {
            item.FindProperty("_name").stringValue = garment.DisplayName;
            item.FindProperty("_asset").objectReferenceValue = reference;
            item.FindProperty("_weight").floatValue = ItemSizeWeightTable.WeightOf("Clothing/" + garment.Name);
            item.FindProperty("_sizeClass").enumValueIndex = (int)ItemSizeWeightTable.SizeOf("Clothing/" + garment.Name);
            if (attachment != null)
            {
                item.FindProperty("AttachmentPoint").objectReferenceValue = attachment;
                item.FindProperty("AttachmentPointAlt").objectReferenceValue = attachment;
            }
        }

        /// <summary>
        /// The exported meshes have one submesh, so they get one material. Leaving the list empty would keep
        /// the body slot's own materials, and a second material draws the last submesh again on top.
        /// </summary>
        /// <summary>Body renderers a full-cover garment hides, as the hardsuit does (the neck is part of the head).</summary>
        private static readonly string[] CoveredBody =
            { "HumanTorso", "HumanArmLeft", "HumanArmRight", "HumanLegLeft", "HumanLegRight", "UnderwearTop", "UnderwearBottom" };

        private static void SetCloth(GameObject root, (ClothType type, Mesh mesh)[] meshes, Material[] materials, string[] occluded)
        {
            var cloth = root.GetComponent<Cloth>();
            if (cloth == null) cloth = root.AddComponent<Cloth>();
            var so = new SerializedObject(cloth);
            SerializedProperty list = so.FindProperty("_clothMeshes._list");
            list.arraySize = meshes.Length;
            for (int i = 0; i < meshes.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("_key").intValue = (int)meshes[i].type;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("_value").objectReferenceValue = meshes[i].mesh;
            }

            SerializedProperty worn = so.FindProperty("_wornMaterials");
            worn.arraySize = materials.Length;
            for (int i = 0; i < materials.Length; i++) worn.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
            SerializedProperty hidden = so.FindProperty("_occludedBodyRenderers");
            hidden.arraySize = occluded.Length;
            for (int i = 0; i < occluded.Length; i++) hidden.GetArrayElementAtIndex(i).stringValue = occluded[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Finish(GameObject root, Garment garment, string path)
        {
            // Copies of the hardsuit would carry its seal; job clothing never seals.
            foreach (var sealed_ in root.GetComponents<SealedGearItemExtension>()) UnityEngine.Object.DestroyImmediate(sealed_);
            var armor = root.GetComponent<ArmorItemExtension>();
            if (garment.Armor is ArmorProfile profile)
            {
                if (armor == null) armor = root.AddComponent<ArmorItemExtension>();
                var so = new SerializedObject(armor);
                so.FindProperty("_profile.BruteAbsorption").floatValue = profile.BruteAbsorption;
                so.FindProperty("_profile.BurnAbsorption").floatValue = profile.BurnAbsorption;
                so.FindProperty("_profile.MaxIntegrity").floatValue = profile.MaxIntegrity;
                so.FindProperty("_profile.CoveredZones").intValue = (int)profile.CoveredZones;
                so.FindProperty("_profile.BlocksStun").boolValue = profile.BlocksStun;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (armor != null)
            {
                UnityEngine.Object.DestroyImmediate(armor);
            }

            SetRestPose(root, garment);
            if (root.GetComponent<SimpleExaminable>() == null) root.AddComponent<SimpleExaminable>();
            Hardsuit.WireExamine(root, garment.Name, garment.Key);
            StorageContainerPrefabSetup.RebuildNetworkBehaviours(root.GetComponent<FishNet.Object.NetworkObject>());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
    }
}
#endif
