namespace SS3D.UI.StoragePanel
{
    /// <summary>
    /// Stable asset paths for the storage panel surface. Rebuild the committed catalog with
    /// <c>SS3D → Storage Panel → Rebuild Asset Catalog</c>. Mirrors MainHudAssetPaths/
    /// MachineUiAssetPaths — a third copy of the path-catalog pattern is expected per
    /// Documents/architecture/systems/ui-shell.md § Future work (shared catalog helper deferred).
    /// </summary>
    public static class StoragePanelAssetPaths
    {
        public const string ResourcesCatalogName = "StoragePanelAssetCatalog";
        public const string CatalogAssetPath =
            "Assets/Content/Systems/UI/StoragePanel/Resources/StoragePanelAssetCatalog.asset";

        // Reuses the same shared overlay PanelSettings as Main HUD / radial menu rather than a new asset.
        public const string PanelSettings =
            "Assets/Content/Systems/UI/Interactions/RadialInteractionMenu/HudOverlayPanelSettings.asset";

        // @import's the shared token/typography stylesheets itself (see MainHud.uss for the same pattern).
        public const string StoragePanelStyle = "Assets/Content/Systems/UI/StoragePanel/StoragePanel.uss";

        // Shared inventory slot chrome the storage panel builds on (see StorageSlot : InventorySlot).
        public const string InventorySlotStyle =
            "Assets/Content/Systems/UI/MachineInterface/Components/InventorySlot.uss";
    }
}
