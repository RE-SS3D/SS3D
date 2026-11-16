using System;
using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Construction;
using SS3D.Systems.Construction.Editor;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    /// <summary>
    /// S4 of 2026-11_construction-mvp1: the wall ladder (girder → plated → sealed, girder → window), air
    /// through unwelded stages, and wreckage blocking a girder until it is cleared.
    /// </summary>
    public class ConstructionLadderTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private static ConstructionRule Rule(ConstructionStep step) => ConstructionRecipes.Get(step);

        private static TileObjectSo WallSo(string name, bool airtight = true)
        {
            TileObjectSo so = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, name);
            so.genericType = TileObjectGenericType.Wall;
            so.specificType = TileObjectSpecificType.Steel;
            so.airtight = airtight;
            return so;
        }

        private static PlacedTileObject Place(TileMapTestUtilities.MapContext context, TileObjectSo so, Vector3 position)
        {
            Assert.True(context.Map.PlaceTileObject(so, position, Direction.North, true, false, false, out GameObject go));
            return go.GetComponent<PlacedTileObject>();
        }

        [Test]
        public void Ladder_RunsGirderPlatedSealed_AndGirderWindow()
        {
            Assert.AreEqual(ConstructionNames.Girder, Rule(ConstructionStep.PlaceGirder).ResultName);
            Assert.AreEqual(ConstructionNames.Girder, Rule(ConstructionStep.PlateGirder).TargetName);
            Assert.AreEqual(ConstructionNames.GirderPlated, Rule(ConstructionStep.PlateGirder).ResultName);
            Assert.AreEqual(ConstructionNames.GirderPlated, Rule(ConstructionStep.WeldPlated).TargetName);
            Assert.AreEqual(ConstructionNames.Wall, Rule(ConstructionStep.WeldPlated).ResultName);
            Assert.AreEqual(ConstructionNames.Girder, Rule(ConstructionStep.GlazeGirder).TargetName);
            Assert.AreEqual(ConstructionNames.Window, Rule(ConstructionStep.GlazeGirder).ResultName);

            Assert.AreEqual(ConstructionInput.SteelSheet, Rule(ConstructionStep.PlateGirder).Input);
            Assert.AreEqual(ConstructionInput.Welder, Rule(ConstructionStep.WeldPlated).Input, "Only the weld seals");
            Assert.AreEqual(ConstructionInput.GlassSheet, Rule(ConstructionStep.GlazeGirder).Input);
            Assert.Greater(Rule(ConstructionStep.GlazeGirder).Seconds, Rule(ConstructionStep.PlateGirder).Seconds,
                "A window takes slightly longer than plating (§7)");

            float rebuild = Rule(ConstructionStep.PlaceGirder).Seconds + Rule(ConstructionStep.PlateGirder).Seconds
                + Rule(ConstructionStep.WeldPlated).Seconds;
            Assert.That(rebuild, Is.InRange(12f, 18f), "Girder to sealed is about 15 s (§10)");
        }

        [Test]
        public void StageSteps_MatchTheirStageByName()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.PlacePlenum(context, Vector3.zero);
            TileMapTestUtilities.PlacePlenum(context, new Vector3(1, 0, 0));
            PlacedTileObject girder = Place(context, WallSo(ConstructionNames.Girder, airtight: false), Vector3.zero);
            PlacedTileObject plated = Place(context, WallSo(ConstructionNames.GirderPlated, airtight: false), new Vector3(1, 0, 0));

            Assert.True(ConstructionObjectRules.Matches(Rule(ConstructionStep.PlateGirder), girder));
            Assert.True(ConstructionObjectRules.Matches(Rule(ConstructionStep.GlazeGirder), girder));
            Assert.False(ConstructionObjectRules.Matches(Rule(ConstructionStep.WeldPlated), girder), "A bare girder has no seams");
            Assert.True(ConstructionObjectRules.Matches(Rule(ConstructionStep.WeldPlated), plated));
            Assert.False(ConstructionObjectRules.Matches(Rule(ConstructionStep.PlateGirder), plated));
        }

        [Test]
        public void Occupancy_GirderAndPlatedPassAir_SealedWallDoesNot()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            Vector3[] cells = { new(0, 0, 0), new(2, 0, 0), new(4, 0, 0) };
            foreach (Vector3 cell in cells) TileMapTestUtilities.PlacePlenum(context, cell);
            Place(context, WallSo(ConstructionNames.Girder, airtight: false), cells[0]);
            Place(context, WallSo(ConstructionNames.GirderPlated, airtight: false), cells[1]);
            Place(context, WallSo(ConstructionNames.Wall), cells[2]);

            for (int i = 0; i < 2; i++)
            {
                Assert.True(context.Query.TryGetOccupancy(context.Query.WorldToTile(cells[i]), out TileOccupancy open));
                Assert.True(open.HasWall, "Girder stages block movement");
                Assert.False(open.IsAirtight, "Air passes until the seams are welded");
                Assert.AreEqual(0, open.BlockedEdges, "No atmos edge is blocked");
            }

            Assert.True(context.Query.TryGetOccupancy(context.Query.WorldToTile(cells[2]), out TileOccupancy sealedWall));
            Assert.True(sealedWall.IsAirtight);
        }

        [Test]
        public void Occupancy_BareGirderIsSeeThrough_PlatedIsNot()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            Vector3 bare = Vector3.zero, plated = new(2, 0, 0);
            TileMapTestUtilities.PlacePlenum(context, bare);
            TileMapTestUtilities.PlacePlenum(context, plated);
            TileObjectSo girderSo = WallSo(ConstructionNames.Girder, airtight: false);
            girderSo.blocksVision = false;
            Place(context, girderSo, bare);
            Place(context, WallSo(ConstructionNames.GirderPlated, airtight: false), plated);

            Assert.True(context.Query.TryGetOccupancy(context.Query.WorldToTile(bare), out TileOccupancy girder));
            Assert.False(girder.BlocksVision, "You see through a bare girder");
            Assert.True(girder.HasWall, "but cannot walk through it");
            Assert.True(context.Query.TryGetOccupancy(context.Query.WorldToTile(plated), out TileOccupancy platedOccupancy));
            Assert.True(platedOccupancy.BlocksVision);
        }

        [Test]
        public void Girder_GoesOnClearFloorOrBarePlating_ReplacingTheFloorTile()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            Vector3 bare = Vector3.zero, floored = new(1, 0, 0), walled = new(2, 0, 0);
            foreach (Vector3 cell in new[] { bare, floored, walled }) TileMapTestUtilities.PlacePlenum(context, cell);
            Place(context, TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "TestFloor"), floored);
            Place(context, WallSo(ConstructionNames.Wall), walled);
            ConstructionRule girder = Rule(ConstructionStep.PlaceGirder);

            Assert.True(ConstructionCellRules.Matches(context.Map, girder, bare));
            Assert.False(ConstructionCellRules.ReplacesExisting(context.Map, girder, bare));
            Assert.True(ConstructionCellRules.Matches(context.Map, girder, floored));
            Assert.True(ConstructionCellRules.ReplacesExisting(context.Map, girder, floored), "The floor tile gives way");
            Assert.False(ConstructionCellRules.Matches(context.Map, girder, walled), "Not on an existing wall");

            TileObjectSo result = WallSo(ConstructionNames.Girder, airtight: false);
            Assert.IsNull(ConstructionCellRules.Validate(context.Map, girder, floored, result, checkStation: true));
        }

        [Test]
        public void Wreckage_BlocksAGirder_UntilCleared()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            Vector3 cell = new(3, 0, 3);
            TileMapTestUtilities.PlacePlenum(context, cell);
            TileCoord coord = ConstructionCellRules.CoordOf(context.Map, cell);
            bool wreckage = true;
            Func<TileCoord, TileLayer, bool> lookup = (c, layer) => wreckage && c == coord && layer == TileLayer.Turf;
            TileObjectSo girderSo = WallSo(ConstructionNames.Girder, airtight: false);

            Assert.AreEqual(BuildFailMessages.Format(BuildFailReason.WreckageInTheWay),
                ConstructionCellRules.Validate(context.Map, Rule(ConstructionStep.PlaceGirder), cell, girderSo, true, lookup));
            Assert.True(ConstructionCellRules.Matches(context.Map, Rule(ConstructionStep.ClearWreckage), cell, lookup),
                "The crowbar has something to clear");

            wreckage = false;
            Assert.IsNull(ConstructionCellRules.Validate(context.Map, Rule(ConstructionStep.PlaceGirder), cell, girderSo, true, lookup));
            Assert.False(ConstructionCellRules.Matches(context.Map, Rule(ConstructionStep.ClearWreckage), cell, lookup));
        }

        [Test]
        public void ClearingWreckage_ReturnsSomeSteel()
        {
            ConstructionRule clear = Rule(ConstructionStep.ClearWreckage);
            Assert.AreEqual(ConstructionInput.Crowbar, clear.Input);
            Assert.AreEqual(ConstructionNames.SteelSheetItem, clear.ReturnItemName);
            Assert.Greater(clear.ReturnCount, 0);
            Assert.LessOrEqual(clear.ReturnCount, Rule(ConstructionStep.PlaceGirder).Cost + Rule(ConstructionStep.PlateGirder).Cost,
                "Salvage gives back some of what went in, not more (§8)");
        }

        [Test]
        public void StagePrefabs_ContainOnlyTheMeshesTheyShow()
        {
            GameObject girder = AssetDatabase.LoadAssetAtPath<GameObject>(GirderStageRecipe.GirderPrefab);
            GameObject plated = AssetDatabase.LoadAssetAtPath<GameObject>(GirderStageRecipe.PlatedPrefab);
            Assert.IsNotNull(girder);
            Assert.IsNotNull(plated, "Run SS3D → Construction → Run Content Prefab Recipes");
            Assert.AreEqual(1, girder.transform.childCount, "Girder keeps GirderShape only (D1)");
            Assert.IsNotNull(girder.transform.Find(GirderStageRecipe.GirderShape));
            Assert.AreEqual(2, plated.transform.childCount);
            Assert.IsNotNull(plated.transform.Find(GirderStageRecipe.WallSheets));

            Assert.IsFalse(AssetDatabase.LoadAssetAtPath<TileObjectSo>(GirderStageRecipe.GirderTile).airtight);
            Assert.IsFalse(AssetDatabase.LoadAssetAtPath<TileObjectSo>(GirderStageRecipe.PlatedTile).airtight);
            Assert.IsFalse(AssetDatabase.LoadAssetAtPath<TileObjectSo>(GirderStageRecipe.GirderTile).blocksVision,
                "A bare girder is open framework");
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<TileObjectSo>(GirderStageRecipe.PlatedTile).blocksVision);
        }
    }
}
