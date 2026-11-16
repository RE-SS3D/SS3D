using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Containers.Editor;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Stamina;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// Guards plans/item-size-weight-balance.plan.md: <see cref="ItemSizeWeightTable"/> is the source of truth
    /// for item size tier and weight, and the committed prefabs must match it. Edit the table and run
    /// SS3D → Inventory → Run Content Prefab Recipes; do not hand-edit the prefabs.
    /// </summary>
    public class ItemSizeWeightTableTests
    {
        private const string LockerPath = "Assets/Content/WorldObjects/Furniture/Storage/Lockers/Locker.prefab";
        private const string HumanTorsoPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/HumanTorso.prefab";

        private static IEnumerable<ItemSizeWeightTable.Entry> Entries => ItemSizeWeightTable.Entries;

        [Test]
        public void EveryItemPrefabUnderItemsRoot_IsInTableOrExempt()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ItemSizeWeightTable.ItemsRoot.TrimEnd('/') });
            HashSet<string> known = new(Entries.Select(entry => entry.PrefabPath));
            known.UnionWith(ItemSizeWeightTable.Exempt.Select(path => ItemSizeWeightTable.ItemsRoot + path + ".prefab"));

            List<string> missing = new();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<Item>() == null || known.Contains(path))
                {
                    continue;
                }

                // A variant (a soda flavour, a coloured jumpsuit) inherits its base's values. It is covered when
                // that base has an entry and the variant does not override the size or weight.
                GameObject baseRoot = prefab;
                while (PrefabUtility.GetPrefabAssetType(baseRoot) == PrefabAssetType.Variant)
                {
                    baseRoot = PrefabUtility.GetCorrespondingObjectFromSource(baseRoot);
                }

                Item item = prefab.GetComponent<Item>();
                Item baseItem = baseRoot.GetComponent<Item>();
                bool coveredByBase = baseRoot != prefab
                    && known.Contains(AssetDatabase.GetAssetPath(baseRoot))
                    && item.SizeClass == baseItem.SizeClass
                    && Mathf.Approximately(item.Weight, baseItem.Weight);
                if (!coveredByBase)
                {
                    missing.Add(path);
                }
            }

            Assert.IsEmpty(missing, "Item prefabs with no size/weight entry. Add them to ItemSizeWeightTable (or Exempt), or stop overriding the base values on a variant:\n" + string.Join("\n", missing));
        }

        [Test]
        public void TablePathsAreUnique()
        {
            Assert.AreEqual(Entries.Count(), Entries.Select(entry => entry.Path).Distinct().Count());
        }

        [Test]
        public void PrefabsMatchTable()
        {
            List<string> drift = new();
            foreach (ItemSizeWeightTable.Entry entry in Entries)
            {
                Item item = LoadItem(entry.PrefabPath);
                if (item.SizeClass != entry.Size || !Mathf.Approximately(item.Weight, entry.Weight))
                {
                    drift.Add($"{entry.Path}: prefab {item.SizeClass}/{item.Weight}, table {entry.Size}/{entry.Weight}");
                }
            }

            Assert.IsEmpty(drift, "Prefabs drifted from ItemSizeWeightTable; run Run Content Prefab Recipes:\n" + string.Join("\n", drift));
        }

        [Test]
        public void Weights_ArePositiveMultiplesOfOneTenth()
        {
            foreach (ItemSizeWeightTable.Entry entry in Entries)
            {
                Assert.Greater(entry.Weight, 0f, entry.Path);
                Assert.AreEqual(Mathf.Round(entry.Weight * 10f), entry.Weight * 10f, 1e-3f,
                    $"{entry.Path} weight {entry.Weight} would show a rounded value in the storage panel.");
            }
        }

        [Test]
        public void TierBands_HoldAndAverageWeightRisesWithTier()
        {
            foreach (ItemSizeWeightTable.Entry entry in Entries)
            {
                switch (entry.Size)
                {
                    case SizeClass.Tiny: Assert.LessOrEqual(entry.Weight, 0.2f, entry.Path); break;
                    case SizeClass.Small: Assert.LessOrEqual(entry.Weight, 0.8f, entry.Path); break;
                    case SizeClass.Normal:
                        // Weapons stay Normal so they fit a backpack, so rifles may reach 3.5.
                        float ceiling = entry.Path.StartsWith("Weapons/") ? 3.5f : 2f;
                        Assert.That(entry.Weight, Is.InRange(0.5f, ceiling), entry.Path);
                        break;
                    case SizeClass.Bulky: Assert.GreaterOrEqual(entry.Weight, 0.5f, entry.Path); break;
                    case SizeClass.Huge: Assert.GreaterOrEqual(entry.Weight, 5f, entry.Path); break;
                }
            }

            float previous = 0f;
            foreach (SizeClass tier in System.Enum.GetValues(typeof(SizeClass)))
            {
                float[] weights = Entries.Where(entry => entry.Size == tier).Select(entry => entry.Weight).ToArray();
                Assert.IsNotEmpty(weights, $"No item uses {tier}");
                float average = weights.Average();
                Assert.Greater(average, previous, $"{tier} items should average heavier than the tier below");
                previous = average;
            }
        }

        [Test]
        public void ContainerGrids_AndCeilings_MatchPlan()
        {
            AssertContainer("Clothing/Backpack", new Vector2Int(4, 3), SizeClass.Normal, 25f);
            AssertContainer("Clothing/Toolbelt", new Vector2Int(5, 1), SizeClass.Small, 6f);
            AssertContainer("Functional/Storage/Objects/Medkits/Medkit", new Vector2Int(3, 2), SizeClass.Normal, 6f);
            AssertContainer("Functional/Storage/Objects/ToolBoxes/ToolboxBlue", new Vector2Int(4, 2), SizeClass.Bulky, 12f);
        }

        [Test]
        public void BackpackFitRules()
        {
            SizeClass ceiling = StorageOf(ItemSizeWeightTable.ItemsRoot + "Clothing/Backpack.prefab").MaxSizeClass;

            Assert.LessOrEqual(ItemSizeWeightTable.SizeOf("Functional/Storage/Objects/Medkits/Medkit"), ceiling, "medkit fits a backpack");
            Assert.LessOrEqual(ItemSizeWeightTable.SizeOf("Functional/Tools/Engineering/Crowbar"), ceiling, "crowbar fits a backpack");
            Assert.LessOrEqual(ItemSizeWeightTable.SizeOf("Clothing/ShoeJackboots"), ceiling, "boots fit a backpack");
            Assert.Greater(ItemSizeWeightTable.SizeOf("Functional/Storage/Objects/ToolBoxes/ToolboxBlue"), ceiling, "toolbox does not fit a backpack");
            Assert.Greater(ItemSizeWeightTable.SizeOf("Clothing/Backpack"), ceiling, "a backpack never fits inside a backpack");
            Assert.Greater(ItemSizeWeightTable.SizeOf("Clothing/Toolbelt"), ceiling, "a toolbelt never fits inside a backpack");
            Assert.Greater(ItemSizeWeightTable.SizeOf("Functional/Generic/Tanks/OxygenTank"), ceiling, "full tanks do not fit a backpack");
        }

        [Test]
        public void PocketsAndToolbelt_TakeOnlySmallAndBelow()
        {
            GameObject torso = AssetDatabase.LoadAssetAtPath<GameObject>(HumanTorsoPath);
            AttachedContainer pocket = torso.GetComponentsInChildren<AttachedContainer>(true).First(c => c.Type == ContainerType.Pocket);

            Assert.AreEqual(SizeClass.Small, pocket.MaxSizeClass);
            Assert.AreEqual(SizeClass.Small, StorageOf(ItemSizeWeightTable.ItemsRoot + "Clothing/Toolbelt.prefab").MaxSizeClass);

            // The crew loadouts put these in pockets (GasTankSetup).
            Assert.LessOrEqual(ItemSizeWeightTable.SizeOf("Functional/Generic/Tanks/EmergencyOxygenTank"), pocket.MaxSizeClass);
            Assert.LessOrEqual(ItemSizeWeightTable.SizeOf("Clothing/FacewearGasMask"), pocket.MaxSizeClass);
        }

        [Test]
        public void Lockers_TakeEveryTier()
        {
            AttachedContainer locker = LoadStorage(LockerPath);
            SizeClass largest = Entries.Max(entry => entry.Size);

            Assert.GreaterOrEqual(locker.MaxSizeClass, largest, "A full gas tank must fit a locker.");
        }

        [Test]
        public void EverydayAndEvaKits_StayInTheirEncumbranceBands()
        {
            float reference = (float)typeof(StaminaController)
                .GetField("ReferenceEncumbranceWeight", BindingFlags.NonPublic | BindingFlags.Static)
                .GetRawConstantValue();

            float everyday = Sum("Clothing/JumpsuitGrey", "Clothing/ShoeHiTops", "Clothing/Backpack", "Clothing/Toolbelt",
                "Functional/Generic/Identification/IDCard", "Functional/Generic/Identification/PDA", "Clothing/Headset");
            float eva = Sum("Clothing/SyndicateHardsuit", "Clothing/SyndicateHardsuitHelmet", "Clothing/SyndicateMagboots",
                "Functional/Generic/Tanks/OxygenTank", "Clothing/JumpsuitGrey", "Clothing/FacewearGasMask",
                "Clothing/GloveInsulated", "Clothing/Backpack");

            Assert.That(everyday / reference, Is.InRange(0.05f, 0.15f), "Everyday kit should cost about 10% of the encumbrance cap.");
            Assert.That(eva / reference, Is.InRange(0.35f, 0.50f), "EVA kit should cost about 40-50% of the encumbrance cap.");
        }

        private static float Sum(params string[] paths) => paths.Sum(ItemSizeWeightTable.WeightOf);

        private static Item LoadItem(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Could not load {prefabPath}");
            Item item = prefab.GetComponent<Item>();
            Assert.IsNotNull(item, $"{prefabPath} has no Item component");
            return item;
        }

        private static AttachedContainer LoadStorage(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Could not load {prefabPath}");
            return StorageOf(prefabPath, prefab);
        }

        private static AttachedContainer StorageOf(string prefabPath) => LoadStorage(prefabPath);

        private static AttachedContainer StorageOf(string prefabPath, GameObject prefab)
        {
            AttachedContainer storage = prefab.GetComponentsInChildren<AttachedContainer>(true)
                .FirstOrDefault(container => container.HasUi && !container.DisplayAsSlotInUI);
            Assert.IsNotNull(storage, $"{prefabPath} has no HasUi storage container");
            return storage;
        }

        private static void AssertContainer(string path, Vector2Int size, SizeClass maxSize, float maxWeight)
        {
            AttachedContainer storage = LoadStorage(ItemSizeWeightTable.ItemsRoot + path + ".prefab");
            Assert.AreEqual(size, storage.Size, $"{path} grid");
            Assert.AreEqual(maxSize, storage.MaxSizeClass, $"{path} ceiling");
            Assert.AreEqual(maxWeight, storage.MaxWeight, 1e-3f, $"{path} readout");
        }
    }
}
