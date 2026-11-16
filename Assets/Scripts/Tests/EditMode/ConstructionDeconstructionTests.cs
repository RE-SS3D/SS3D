using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Construction;
using SS3D.Systems.Construction.Editor;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    /// <summary>
    /// S5 of 2026-11_construction-mvp1: deconstruction runs the ladder backwards from Intact only, takes
    /// about twice the build time, gives material back, and never touches reinforced structures (§9, D10).
    /// </summary>
    public class ConstructionDeconstructionTests
    {
        private static readonly ConstructionStep[] Deconstruction =
        {
            ConstructionStep.CutSeams, ConstructionStep.PullPlate, ConstructionStep.LiftPane, ConstructionStep.DismantleGirder,
        };

        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private static ConstructionRule Rule(ConstructionStep step) => ConstructionRecipes.Get(step);

        private static PlacedTileObject Wall(TileMapTestUtilities.MapContext context, string name, Vector3 position, bool reinforced = false)
        {
            TileMapTestUtilities.PlacePlenum(context, position);
            TileObjectSo so = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, name);
            so.genericType = TileObjectGenericType.Wall;
            so.specificType = TileObjectSpecificType.Steel;
            so.reinforced = reinforced;
            Assert.True(context.Map.PlaceTileObject(so, position, Direction.North, true, false, false, out GameObject go));
            return go.GetComponent<PlacedTileObject>();
        }

        [Test]
        public void ReverseLadder_SealedPlatedGirderOpen_AndWindowGirder()
        {
            Assert.AreEqual((ConstructionNames.Wall, ConstructionNames.GirderPlated),
                (Rule(ConstructionStep.CutSeams).TargetName, Rule(ConstructionStep.CutSeams).ResultName));
            Assert.AreEqual((ConstructionNames.GirderPlated, ConstructionNames.Girder),
                (Rule(ConstructionStep.PullPlate).TargetName, Rule(ConstructionStep.PullPlate).ResultName));
            Assert.AreEqual((ConstructionNames.Window, ConstructionNames.Girder),
                (Rule(ConstructionStep.LiftPane).TargetName, Rule(ConstructionStep.LiftPane).ResultName));
            Assert.AreEqual(ConstructionNames.Girder, Rule(ConstructionStep.DismantleGirder).TargetName);
            Assert.AreEqual(ConstructionOutcome.Remove, Rule(ConstructionStep.DismantleGirder).Outcome, "A cut girder leaves open floor");

            Assert.AreEqual(ConstructionInput.Welder, Rule(ConstructionStep.CutSeams).Input);
            Assert.AreEqual(ConstructionInput.Crowbar, Rule(ConstructionStep.PullPlate).Input);
            Assert.AreEqual(ConstructionInput.Crowbar, Rule(ConstructionStep.LiftPane).Input);
            Assert.AreEqual(ConstructionInput.Welder, Rule(ConstructionStep.DismantleGirder).Input);
        }

        [TestCase(ConstructionStep.CutSeams, ConstructionStep.WeldPlated)]
        [TestCase(ConstructionStep.PullPlate, ConstructionStep.PlateGirder)]
        [TestCase(ConstructionStep.LiftPane, ConstructionStep.GlazeGirder)]
        [TestCase(ConstructionStep.DismantleGirder, ConstructionStep.PlaceGirder)]
        public void Deconstructing_TakesAboutTwiceTheBuild(ConstructionStep undo, ConstructionStep build)
        {
            Assert.That(Rule(undo).Seconds, Is.EqualTo(Rule(build).Seconds * 2f).Within(Rule(build).Seconds * 0.25f));
        }

        [Test]
        public void Returns_GiveBackWhatTheStageCost()
        {
            Assert.IsNull(Rule(ConstructionStep.CutSeams).ReturnItemName, "Cutting seams returns nothing");
            Assert.AreEqual(ConstructionNames.SteelSheetItem, Rule(ConstructionStep.PullPlate).ReturnItemName);
            Assert.AreEqual(Rule(ConstructionStep.PlateGirder).Cost, Rule(ConstructionStep.PullPlate).ReturnCount);
            Assert.AreEqual(ConstructionNames.GlassSheetItem, Rule(ConstructionStep.LiftPane).ReturnItemName);
            Assert.AreEqual(Rule(ConstructionStep.GlazeGirder).Cost, Rule(ConstructionStep.LiftPane).ReturnCount);
            Assert.AreEqual(ConstructionNames.SteelSheetItem, Rule(ConstructionStep.DismantleGirder).ReturnItemName);
            Assert.AreEqual(Rule(ConstructionStep.PlaceGirder).Cost, Rule(ConstructionStep.DismantleGirder).ReturnCount);
        }

        [Test]
        public void Deconstruction_IsRefusedOnDamagedStructures()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            PlacedTileObject wall = Wall(context, ConstructionNames.Wall, new Vector3(2, 0, 2));
            ConstructionRule cut = Rule(ConstructionStep.CutSeams);

            Assert.True(ConstructionObjectRules.Matches(cut, wall), "Intact wall can be cut");
            wall.ServerSetIntegrity(70f, StructuralIntegrityStage.Damaged);
            Assert.False(ConstructionObjectRules.Matches(cut, wall), "Repair first (§9)");
            Assert.True(ConstructionObjectRules.Matches(Rule(ConstructionStep.RepairStructure), wall));
        }

        [Test]
        public void Deconstruction_IsRefusedOnReinforced_RepairIsNot()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            // Named like a plain wall so only the flag can refuse it.
            PlacedTileObject vault = Wall(context, ConstructionNames.Wall, new Vector3(2, 0, 2), reinforced: true);

            foreach (ConstructionStep step in Deconstruction)
            {
                Assert.True(Rule(step).RefusesReinforced, step.ToString());
            }

            Assert.False(ConstructionObjectRules.Matches(Rule(ConstructionStep.CutSeams), vault));
            vault.ServerSetIntegrity(30f, StructuralIntegrityStage.Cracked);
            Assert.True(ConstructionObjectRules.Matches(Rule(ConstructionStep.RepairStructure), vault), "Reinforced walls can be repaired");
        }

        [Test]
        public void NoRule_BuildsAReinforcedStructure()
        {
            HashSet<string> results = new(ConstructionRecipes.Rules.Where(r => r.ResultName != null).Select(r => r.ResultName));
            Assert.False(results.Contains("SteelWallReinforced"));
            Assert.False(results.Contains("SteelWindowReinforced"));
        }

        [Test]
        public void ReinforcedAssets_MatchTheRecipe()
        {
            foreach ((string path, float maxIntegrity) in ConstructionReinforcedRecipe.Targets)
            {
                TileObjectSo tile = AssetDatabase.LoadAssetAtPath<TileObjectSo>(path);
                Assert.IsNotNull(tile, path);
                Assert.IsTrue(tile.reinforced, $"{path}: run SS3D → Construction → Run Content Prefab Recipes");
                if (maxIntegrity > 0f)
                {
                    Assert.AreEqual(maxIntegrity, StructuralIntegrityRules.ResolveMaxIntegrity(tile), 0.01f, path);
                    Assert.Greater(maxIntegrity, StructuralIntegrityRules.ResolveMaxIntegrity(
                        AssetDatabase.LoadAssetAtPath<TileObjectSo>("Assets/Content/Data/TileMap/Resources/Structures/Walls/SteelWall.asset")),
                        "The vault wall outlasts a plain wall (D10)");
                }
            }
        }
    }
}
