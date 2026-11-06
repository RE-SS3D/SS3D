#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Audio.Editor
{
    /// <summary>
    /// Domain aggregator for audio content (tier B). Gives item prefabs a <see cref="NoisyCollision"/>
    /// of the right material. A new item goes in <see cref="Materials"/>, or in
    /// <see cref="SilentItems"/> if it's soft, or <c>ItemPrefabs_MatchTheRecipeLists</c> fails.
    /// A prefab variant takes the entry of the nearest base that has one.
    /// </summary>
    public static class AudioContentPrefabRecipes
    {
        private const string ItemsFolder = "Assets/Content/WorldObjects/Items";

        private static readonly Dictionary<string, ItemCollisionMaterial> Materials = new()
        {
            { "Crowbar", ItemCollisionMaterial.Metal },
            { "FlashlightBlue", ItemCollisionMaterial.Metal },
            { "FloorTile", ItemCollisionMaterial.Metal },
            { "Hatchet", ItemCollisionMaterial.Metal },
            { "KitchenKnife", ItemCollisionMaterial.Metal },
            { "M4", ItemCollisionMaterial.Metal },
            { "MatterBin", ItemCollisionMaterial.Metal },
            { "MicroLaser", ItemCollisionMaterial.Metal },
            { "Multitool", ItemCollisionMaterial.Metal },
            { "PowerCell", ItemCollisionMaterial.Metal },
            { "ScrewdriverBlue", ItemCollisionMaterial.Metal },
            { "SteelReinforcedSheet", ItemCollisionMaterial.Metal },
            { "SteelRod", ItemCollisionMaterial.Metal },
            { "SteelSheet", ItemCollisionMaterial.Metal },
            { "Welder", ItemCollisionMaterial.Metal },
            { "Wirecutters", ItemCollisionMaterial.Metal },
            { "Wrench", ItemCollisionMaterial.Metal },
            { "EmergencyOxygenTank", ItemCollisionMaterial.Metal },
            { "OxygenTank", ItemCollisionMaterial.HeavyMetal },
            { "PlasmaTank", ItemCollisionMaterial.HeavyMetal },
            { "SyndicateMagboots", ItemCollisionMaterial.Metal },
            { "ToolboxBlue", ItemCollisionMaterial.HeavyMetal },
            { "SodaCanGeneric", ItemCollisionMaterial.Can },
            { "WoodLog", ItemCollisionMaterial.Wood },
            { "WoodPlank", ItemCollisionMaterial.Wood },
            { "BikeHorn", ItemCollisionMaterial.Plastic },
            { "Boombox", ItemCollisionMaterial.Plastic },
            { "ElectricalCable", ItemCollisionMaterial.Plastic },
            { "ElectricalWire", ItemCollisionMaterial.Plastic },
            { "FacewearGasMask", ItemCollisionMaterial.Plastic },
            { "Headset", ItemCollisionMaterial.Plastic },
            { "HealthScanner", ItemCollisionMaterial.Plastic },
            { "IDCard", ItemCollisionMaterial.Plastic },
            { "Medkit", ItemCollisionMaterial.Plastic },
            { "NuclearAuthenticationDisk", ItemCollisionMaterial.Plastic },
            { "PDA", ItemCollisionMaterial.Plastic },
            { "SecurityIDCard", ItemCollisionMaterial.Plastic },
            { "SecurityPDA", ItemCollisionMaterial.Plastic },
            { "Sunglasses", ItemCollisionMaterial.Plastic },
            { "GlassReinforcedSheet", ItemCollisionMaterial.Glass },
            { "GlassShard", ItemCollisionMaterial.Glass },
            { "GlassSheet", ItemCollisionMaterial.Glass },
            { "Mug", ItemCollisionMaterial.Glass },
        };

        private static readonly HashSet<string> SilentItems = new()
        {
            "Backpack", "BrutePatch", "BurnPatch", "DonkPocket", "Doodad", "GloveInsulated", "JumpsuitGrey",
            "MedicalPatch", "Poster", "ShoeHiTops", "ShoeJackboots", "SyndicateHardsuit", "SyndicateHardsuitHelmet",
            "Toolbelt", "TruckerCap",
        };

        private static readonly Dictionary<string, AmbientLoopKind> MachineLoops = new()
        {
            { "Assets/Content/WorldObjects/Furniture/Machines/Atmospherics/Vent/Vent.prefab", AmbientLoopKind.Vent },
            { "Assets/Content/WorldObjects/Furniture/Machines/Atmospherics/Scrubber/Scrubber.prefab", AmbientLoopKind.Scrubber },
            { "Assets/Content/WorldObjects/Furniture/Machines/Atmospherics/Scrubber/ScrubberIntake.prefab", AmbientLoopKind.Scrubber },
            { "Assets/Content/WorldObjects/Furniture/Machines/Atmospherics/GasPump/GasPump.prefab", AmbientLoopKind.GasPump },
            { "Assets/Content/WorldObjects/Structures/Pipes/Atmospherics/Devices/AtmosPumpVolume.prefab", AmbientLoopKind.GasPump },
            { "Assets/Content/WorldObjects/Furniture/Machines/Engineering/SMES.prefab", AmbientLoopKind.Smes },
            { "Assets/Content/WorldObjects/Furniture/Machines/Vendors/VendorHotDrinks.prefab", AmbientLoopKind.Vendor },
            { "Assets/Content/WorldObjects/Furniture/Machines/Vendors/VendorRobustSoftdrinks.prefab", AmbientLoopKind.Vendor },
            { "Assets/Content/WorldObjects/Furniture/Machines/Vendors/VendorSecTech.prefab", AmbientLoopKind.Vendor },
            { "Assets/Content/WorldObjects/Furniture/Machines/Vendors/VendorYouTool.prefab", AmbientLoopKind.Vendor },
            { "Assets/Content/WorldObjects/Structures/WallMounts/APC.prefab", AmbientLoopKind.Apc },
            { "Assets/Content/WorldObjects/Structures/WallMounts/AirAlarm.prefab", AmbientLoopKind.AirAlarm },
        };

        [MenuItem("SS3D/Audio/Run Content Prefab Recipes")]
        public static void RunAllMenu()
        {
            int items = SetupItemCollisions();
            int machines = SetupMachineLoops();
            EditorUtility.DisplayDialog("Audio Content Prefab Recipes", $"Item prefabs updated: {items}. Machine prefabs updated: {machines}.", "OK");
        }

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Audio.Editor.AudioContentPrefabRecipes.RunAllBatch</c></summary>
        public static void RunAllBatch()
        {
            Debug.Log($"[AudioContentPrefabRecipes] items={SetupItemCollisions()} machines={SetupMachineLoops()}");
            if (UnityEngine.Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>Gives each listed item its material. Returns how many prefabs changed.</summary>
        public static int SetupItemCollisions()
        {
            int updated = 0;
            foreach (string path in FindItemPrefabs())
            {
                if (Materials.TryGetValue(Path.GetFileNameWithoutExtension(path), out ItemCollisionMaterial material)
                    && SetMaterial(path, material))
                {
                    updated++;
                }
            }

            return updated;
        }

        /// <summary>Gives each listed machine its loop. Returns how many prefabs changed.</summary>
        public static int SetupMachineLoops()
        {
            int updated = 0;
            foreach (KeyValuePair<string, AmbientLoopKind> machine in MachineLoops)
            {
                if (SetLoop(machine.Key, machine.Value))
                {
                    updated++;
                }
            }

            return updated;
        }

        /// <summary>Describes each machine prefab that doesn't match <see cref="MachineLoops"/>.</summary>
        public static List<string> FindMachineLoopDrift()
        {
            var drift = new List<string>();
            foreach (KeyValuePair<string, AmbientLoopKind> machine in MachineLoops)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(machine.Key);
                if (prefab == null)
                {
                    drift.Add($"{machine.Key} doesn't exist");
                    continue;
                }

                AmbientLoopSource source = prefab.GetComponent<AmbientLoopSource>();
                if (source == null || source.Kind != machine.Value)
                {
                    drift.Add($"{prefab.name} should have a {machine.Value} loop; run the audio prefab recipes");
                }

                if (prefab.GetComponentInChildren<IAmbientLoopState>(true) == null)
                {
                    drift.Add($"{prefab.name} has nothing that says when its loop runs");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Content/WorldObjects" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab.GetComponentInChildren<IAmbientLoopState>(true) != null && !IsListedMachine(prefab))
                {
                    drift.Add($"{prefab.name} can run a loop but isn't in {nameof(MachineLoops)}");
                }
            }

            return drift;
        }

        /// <summary>The prefab or its nearest base is in <see cref="MachineLoops"/>.</summary>
        private static bool IsListedMachine(GameObject prefab)
        {
            for (GameObject current = prefab; current != null; current = PrefabUtility.GetCorrespondingObjectFromSource(current))
            {
                if (MachineLoops.ContainsKey(AssetDatabase.GetAssetPath(current)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Describes each item prefab that doesn't match the lists above.</summary>
        public static List<string> FindDrift()
        {
            var drift = new List<string>();
            foreach (string path in FindItemPrefabs())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                NoisyCollision noisy = prefab.GetComponent<NoisyCollision>();
                string listedAs = FindListedName(prefab);
                if (listedAs == null)
                {
                    drift.Add($"{prefab.name} is in neither list in {nameof(AudioContentPrefabRecipes)}");
                }
                else if (SilentItems.Contains(listedAs))
                {
                    if (noisy != null)
                    {
                        drift.Add($"{prefab.name} is silent but has {nameof(NoisyCollision)}");
                    }
                }
                else if (noisy == null || noisy.Material != Materials[listedAs])
                {
                    drift.Add($"{prefab.name} should be {Materials[listedAs]}; run the audio prefab recipes");
                }
            }

            return drift;
        }

        /// <summary>The prefab's name, or its nearest base's, in either list. Null if neither has it.</summary>
        private static string FindListedName(GameObject prefab)
        {
            for (GameObject current = prefab; current != null; current = PrefabUtility.GetCorrespondingObjectFromSource(current))
            {
                if (Materials.ContainsKey(current.name) || SilentItems.Contains(current.name))
                {
                    return current.name;
                }
            }

            return null;
        }

        private static IEnumerable<string> FindItemPrefabs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ItemsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Item>() != null)
                {
                    yield return path;
                }
            }
        }

        private static bool SetLoop(string path, AmbientLoopKind kind)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!root.TryGetComponent(out AmbientLoopSource source))
                {
                    source = root.AddComponent<AmbientLoopSource>();
                }
                else if (source.Kind == kind)
                {
                    return false;
                }

                var serialized = new SerializedObject(source);
                serialized.FindProperty("_kind").enumValueIndex = (int)kind;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool SetMaterial(string path, ItemCollisionMaterial material)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!root.TryGetComponent(out NoisyCollision noisy))
                {
                    noisy = root.AddComponent<NoisyCollision>();
                }
                else if (noisy.Material == material)
                {
                    return false;
                }

                var serialized = new SerializedObject(noisy);
                serialized.FindProperty("_material").enumValueIndex = (int)material;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
