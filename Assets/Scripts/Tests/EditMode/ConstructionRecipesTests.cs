using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Construction;
using SS3D.Systems.Construction.Editor;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// D4/D5/D12 of 2026-11_construction-mvp1: every rule in <see cref="ConstructionRecipes"/> resolves to real
    /// assets, and the committed construction items match <see cref="ConstructionItemRecipe"/>. Edit the
    /// tables and run SS3D → Construction → Run Content Prefab Recipes; do not hand-edit the prefabs.
    /// </summary>
    public class ConstructionRecipesTests
    {
        private static Dictionary<string, GenericObjectSo> LoadCatalog()
        {
            Dictionary<string, GenericObjectSo> byName = new();
            foreach (GenericObjectSo asset in Resources.LoadAll<GenericObjectSo>(""))
            {
                if (asset != null && asset.PrefabAsset != null)
                {
                    byName[asset.NameString] = asset;
                }
            }

            return byName;
        }

        [Test]
        public void EveryReferencedName_ResolvesToACatalogAsset()
        {
            Dictionary<string, GenericObjectSo> catalog = LoadCatalog();
            List<string> missing = ConstructionRecipes.ReferencedNames().Distinct()
                .Where(name => !catalog.ContainsKey(name)).ToList();

            Assert.IsEmpty(missing, "Construction rules name assets that do not exist: " + string.Join(", ", missing));
        }

        [Test]
        public void PlacedResults_AreTileObjectsOnTheRuleLayer()
        {
            Dictionary<string, GenericObjectSo> catalog = LoadCatalog();
            foreach (ConstructionRule rule in ConstructionRecipes.Rules.Where(r => r.Outcome == ConstructionOutcome.Place))
            {
                Assert.IsTrue(catalog.TryGetValue(rule.ResultName, out GenericObjectSo asset), rule.GenericName);
                TileObjectSo tile = asset as TileObjectSo;
                Assert.IsNotNull(tile, $"{rule.GenericName}: {rule.ResultName} is not a tile object");
                Assert.AreEqual(rule.Layer, tile.layer, $"{rule.GenericName}: result layer");
            }
        }

        [Test]
        public void Returns_AreItems()
        {
            Dictionary<string, GenericObjectSo> catalog = LoadCatalog();
            foreach (ConstructionRule rule in ConstructionRecipes.Rules.Where(r => r.ReturnItemName != null))
            {
                Assert.IsInstanceOf<ItemObjectSo>(catalog[rule.ReturnItemName], rule.GenericName);
                Assert.Greater(rule.ReturnCount, 0, rule.GenericName);
            }
        }

        [Test]
        public void Rules_HaveUniqueIdsAndSaneCosts()
        {
            Assert.AreEqual(ConstructionRecipes.Rules.Count,
                ConstructionRecipes.Rules.Select(r => r.GenericName).Distinct().Count(), "duplicate GenericName");
            Assert.AreEqual(ConstructionRecipes.Rules.Count,
                ConstructionRecipes.Rules.Select(r => r.Step).Distinct().Count(), "duplicate Step");

            foreach (ConstructionRule rule in ConstructionRecipes.Rules)
            {
                Assert.Greater(rule.Seconds, 0f, rule.GenericName);
                if (ConstructionInputs.IsMaterial(rule.Input))
                {
                    Assert.Greater(rule.Cost, 0, $"{rule.GenericName}: a material step must cost something");
                }
                else
                {
                    Assert.AreEqual(0, rule.Cost, $"{rule.GenericName}: tools are not spent");
                }
            }
        }

        [Test]
        public void MaterialPrefabs_MatchTheRecipe()
        {
            foreach ((string path, ConstructionMaterialKind kind, int maxStack, int initial) in ConstructionItemRecipe.Materials)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, path);
                Item item = prefab.GetComponent<Item>();
                Assert.AreEqual(maxStack, item.MaxStackSize, $"{path} stack limit");
                Assert.AreEqual(initial, item.InitialStackCount, $"{path} initial count");
                Assert.IsTrue(prefab.TryGetComponent(out ConstructionMaterial material), $"{path} has no ConstructionMaterial");
                Assert.AreEqual(kind, material.Kind, path);
            }
        }

        [Test]
        public void ToolPrefabs_MatchTheRecipe()
        {
            foreach ((string path, ConstructionToolKind kind) in ConstructionItemRecipe.Tools)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, path);
                Assert.IsTrue(ConstructionTool.Is(prefab.GetComponent<Item>(), kind), $"{path} is not a {kind}");
            }
        }
    }
}
