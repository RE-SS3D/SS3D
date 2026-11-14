using NUnit.Framework;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.MapEditor.UI;
using SS3D.Systems.Tile.TileMapCreator;

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
            Assert.IsTrue(viewModel.IsLayerCategoryVisible(TileLayerCategory.GasPipes));
            Assert.IsTrue(viewModel.IsLayerCategoryVisible(TileLayerCategory.Cables));
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
