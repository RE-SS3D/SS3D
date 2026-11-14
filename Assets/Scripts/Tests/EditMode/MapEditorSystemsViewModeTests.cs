using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.MapEditor.UI;
using SS3D.Systems.Tile.TileMapCreator;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Map Editor systems view mode (creative-mode §4 "Who and when"): on by itself on Gas Pipes and Cables, pinned by
    /// the toolbar toggle from any tab, x-ray layers dimmed while on and restored by the Lower tabs' rule.
    /// </summary>
    public sealed class MapEditorSystemsViewModeTests
    {
        private static MapEditorViewModel Open(MapEditorSubcategory subcategory)
        {
            MapEditorViewModel viewModel = new();
            viewModel.EnsureLayerDefaults();
            viewModel.OpenSubTab(subcategory);
            return viewModel;
        }

        private static void AssertXRay(MapEditorViewModel viewModel, bool dimmed)
        {
            foreach (TileLayerCategory layer in MapEditorViewModel.SystemsViewXRayLayers)
                Assert.AreEqual(!dimmed, viewModel.IsLayerCategoryVisible(layer), layer.ToString());
            // These all run the Atmos view, which also dims the pipes' own paint.
            Assert.AreEqual(!dimmed, viewModel.IsLayerCategoryVisible(TileLayerCategory.GasPipes), "GasPipes");
            Assert.IsTrue(viewModel.IsLayerCategoryVisible(TileLayerCategory.Cables));
        }

        [Test]
        public void AtmosView_DimsThePipes_ButAPipeStillWinsThePickOverAWireUnderIt()
        {
            MapEditorViewModel viewModel = Open(MapEditorSubcategory.Machines);
            viewModel.SetSystemsView(SystemsViewKind.Atmos);
            Assert.IsFalse(viewModel.IsLayerCategoryVisible(TileLayerCategory.GasPipes));
            Assert.IsTrue(viewModel.IsLayerCategoryVisible(TileLayerCategory.Cables));

            List<GameObject> created = new();
            try
            {
                ITileLocation[] locations = new ITileLocation[System.Enum.GetValues(typeof(TileLayer)).Length];
                PlacedTileObject pipe = PlaceAt(locations, TileLayer.PipeLeft, created);
                PlaceAt(locations, TileLayer.Wire, created);

                Assert.IsTrue(MapEditorCursorPick.TryPickFromLocations(locations, viewModel, out PlacedTileObject picked));
                Assert.AreSame(pipe, picked, "The view's own layer stays first to pick while it is dimmed");
            }
            finally
            {
                foreach (GameObject go in created)
                    Object.DestroyImmediate(go);
            }
        }

        private static PlacedTileObject PlaceAt(ITileLocation[] locations, TileLayer layer, List<GameObject> created)
        {
            GameObject go = new(layer.ToString());
            created.Add(go);
            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            locations[(int)layer] = TileHelper.CreateTileLocation(layer, 0, 0);
            locations[(int)layer].AddPlacedObject(placed);
            return placed;
        }

        [Test]
        public void CursorLabel_KeepsThePlacingLabel_AndAddsTheHoveredLine()
        {
            Assert.AreEqual("Placing Pipe\nSupply 3", MapEditorSystemsViewClient.CursorLabel("Placing Pipe", "Supply 3"));
            Assert.AreEqual("Supply 3", MapEditorSystemsViewClient.CursorLabel(string.Empty, "Supply 3"));
            Assert.AreEqual("Placing Pipe", MapEditorSystemsViewClient.CursorLabel("Placing Pipe", string.Empty));
        }

        [Test]
        public void SwitchingFromAtmosToPower_GivesThePipesBack()
        {
            MapEditorViewModel viewModel = Open(MapEditorSubcategory.Machines);
            viewModel.SetSystemsView(SystemsViewKind.Atmos);
            Assert.IsFalse(viewModel.IsLayerCategoryVisible(TileLayerCategory.GasPipes));

            viewModel.SetSystemsView(SystemsViewKind.Power);

            Assert.IsTrue(viewModel.IsLayerCategoryVisible(TileLayerCategory.GasPipes));
            Assert.IsFalse(viewModel.IsLayerCategoryVisible(TileLayerCategory.Furniture), "the shared x-ray stays");
        }

        [Test]
        public void LowerRunTabs_TurnTheirViewOnByThemselves()
        {
            Assert.AreEqual(SystemsViewKind.Atmos, Open(MapEditorSubcategory.GasPipes).ActiveSystemsView);
            Assert.AreEqual(SystemsViewKind.Power, Open(MapEditorSubcategory.Cables).ActiveSystemsView);
            Assert.AreEqual(SystemsViewKind.Off, Open(MapEditorSubcategory.DisposalPipes).ActiveSystemsView);
            Assert.AreEqual(SystemsViewKind.Off, Open(MapEditorSubcategory.Machines).ActiveSystemsView);
        }

        [Test]
        public void AutoView_DimsXRayLayers_AndLeavingTheTabRestoresThem()
        {
            MapEditorViewModel viewModel = Open(MapEditorSubcategory.GasPipes);
            AssertXRay(viewModel, dimmed: true);

            viewModel.OpenSubTab(MapEditorSubcategory.Machines);

            Assert.AreEqual(SystemsViewKind.Off, viewModel.ActiveSystemsView);
            AssertXRay(viewModel, dimmed: false);
        }

        [Test]
        public void PinnedView_StaysOnFromAnyTab_UntilTurnedOff()
        {
            MapEditorViewModel viewModel = Open(MapEditorSubcategory.Machines);

            viewModel.SetSystemsView(SystemsViewKind.Atmos);
            Assert.AreEqual(SystemsViewKind.Atmos, viewModel.ActiveSystemsView);
            AssertXRay(viewModel, dimmed: true);

            viewModel.OpenSubTab(MapEditorSubcategory.Floors);
            Assert.AreEqual(SystemsViewKind.Atmos, viewModel.ActiveSystemsView);
            AssertXRay(viewModel, dimmed: true);

            viewModel.SetSystemsView(SystemsViewKind.Off);
            Assert.AreEqual(SystemsViewKind.Off, viewModel.ActiveSystemsView);
            AssertXRay(viewModel, dimmed: false);
        }

        [Test]
        public void PinnedView_WinsOverTheTabsOwn()
        {
            MapEditorViewModel viewModel = Open(MapEditorSubcategory.Machines);
            viewModel.SetSystemsView(SystemsViewKind.Power);

            viewModel.OpenSubTab(MapEditorSubcategory.GasPipes);

            Assert.AreEqual(SystemsViewKind.Power, viewModel.ActiveSystemsView);
        }

        [Test]
        public void TurningTheTabsViewOff_HoldsUntilAnotherSubTabOpens()
        {
            MapEditorViewModel viewModel = Open(MapEditorSubcategory.GasPipes);

            viewModel.SetSystemsView(SystemsViewKind.Off);
            Assert.AreEqual(SystemsViewKind.Off, viewModel.ActiveSystemsView);
            // Gas Pipes still hides Turfs by its own rule; the rest of the x-ray comes back.
            Assert.IsFalse(viewModel.IsLayerCategoryVisible(TileLayerCategory.Turfs));
            Assert.IsTrue(viewModel.IsLayerCategoryVisible(TileLayerCategory.Furniture));

            viewModel.SetSubcategory(MapEditorSubcategory.GasPipes);
            Assert.AreEqual(SystemsViewKind.Off, viewModel.ActiveSystemsView);

            viewModel.OpenSubTab(MapEditorSubcategory.Cables);
            Assert.AreEqual(SystemsViewKind.Power, viewModel.ActiveSystemsView);
            viewModel.OpenSubTab(MapEditorSubcategory.GasPipes);
            Assert.AreEqual(SystemsViewKind.Atmos, viewModel.ActiveSystemsView);
        }

        [Test]
        public void LayerToggledWhileOn_IsTheAuthorsAndIsNotRestored()
        {
            MapEditorViewModel viewModel = Open(MapEditorSubcategory.Machines);
            viewModel.SetLayerCategoryVisible(TileLayerCategory.Items, false);
            viewModel.SetSystemsView(SystemsViewKind.Atmos);

            // Shown again by hand while the view dims it, then the view turns off.
            viewModel.SetLayerCategoryVisible(TileLayerCategory.Furniture, true);
            viewModel.SetLayerCategoryVisible(TileLayerCategory.WallMounts, false);
            viewModel.SetSystemsView(SystemsViewKind.Off);

            Assert.IsFalse(viewModel.IsLayerCategoryVisible(TileLayerCategory.Items), "hidden before the view: stays hidden");
            Assert.IsTrue(viewModel.IsLayerCategoryVisible(TileLayerCategory.Furniture));
            Assert.IsFalse(viewModel.IsLayerCategoryVisible(TileLayerCategory.WallMounts), "toggled meanwhile: left as set");
            Assert.IsTrue(viewModel.IsLayerCategoryVisible(TileLayerCategory.Turfs));
        }

        [Test]
        public void LowerTabTurfRule_StillHoldsWithoutAView()
        {
            MapEditorViewModel viewModel = Open(MapEditorSubcategory.DisposalPipes);
            Assert.IsFalse(viewModel.IsLayerCategoryVisible(TileLayerCategory.Turfs));
            Assert.IsTrue(viewModel.IsLayerCategoryVisible(TileLayerCategory.Furniture));

            viewModel.OpenSubTab(MapEditorSubcategory.Floors);
            Assert.IsTrue(viewModel.IsLayerCategoryVisible(TileLayerCategory.Turfs));
        }
    }
}
