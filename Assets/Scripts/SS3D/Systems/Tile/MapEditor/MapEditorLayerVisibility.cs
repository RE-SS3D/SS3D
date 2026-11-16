using SS3D.Systems.Tile.MapEditor.UI;
using SS3D.Systems.Tile.TileMapCreator;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Maps map editor layer toggles to <see cref="TileLayerCategory"/> visibility groups.
    /// </summary>
    public static class MapEditorLayerVisibility
    {
        public static void Apply(MapEditorViewModel viewModel) => Apply(viewModel, onlyChanged: false);

        /// <summary>
        /// Applies only the groups whose visibility differs from <paramref name="viewModel"/>: a tab switch or a
        /// systems view toggle changes a few groups, and re-dimming every group walks every chunk.
        /// </summary>
        public static void Sync(MapEditorViewModel viewModel) => Apply(viewModel, onlyChanged: true);

        private static void Apply(MapEditorViewModel viewModel, bool onlyChanged)
        {
            if (viewModel == null)
                return;

            foreach (TileLayerCategory category in TileLayerCategoryMapping.AllCategories)
            {
                if (category == TileLayerCategory.Areas) continue; // Separate authoring overlay.
                bool visible = viewModel.IsLayerCategoryVisible(category);
                if (!onlyChanged || TileLayerVisibilityService.IsGroupVisible(category) != visible)
                    TileLayerVisibilityService.SetGroupVisible(category, visible);
            }
        }

        public static void Activate() => TileLayerVisibilityService.Activate();

        public static void Deactivate() => TileLayerVisibilityService.Deactivate();
    }
}
