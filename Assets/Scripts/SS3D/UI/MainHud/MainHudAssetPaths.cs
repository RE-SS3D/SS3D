namespace SS3D.UI.MainHud
{
    /// <summary>
    /// Stable asset paths for the Main HUD. Rebuild the committed catalog with
    /// <c>SS3D → Data → Rebuild All UI Catalogs</c>.
    /// </summary>
    public static class MainHudAssetPaths
    {
        public const string ResourcesCatalogName = "MainHudAssetCatalog";
        public const string CatalogAssetPath =
            "Assets/Content/Systems/UI/MainHud/Resources/MainHudAssetCatalog.asset";

        public const string PanelSettings =
            "Assets/Content/Systems/UI/Interactions/RadialInteractionMenu/HudOverlayPanelSettings.asset";

        public const string MainHudStyle = "Assets/Content/Systems/UI/MainHud/MainHud.uss";
        public const string AlertIconStackStyle =
            "Assets/Content/Systems/UI/MainHud/Components/AlertIconStack.uss";
        public const string IntentModuleStyle =
            "Assets/Content/Systems/UI/MainHud/Components/IntentModule.uss";
        public const string HandsGearStripStyle =
            "Assets/Content/Systems/UI/MainHud/Components/HandsGearStrip.uss";
        public const string EquipmentGridStyle =
            "Assets/Content/Systems/UI/MainHud/Components/EquipmentGrid.uss";
        public const string InventorySlotStyle =
            "Assets/Content/Systems/UI/MachineInterface/Components/InventorySlot.uss";

        public const string IconRoot = "Assets/Art/Icons/Inventory/";
        public const string AlertIconRoot = "Assets/Art/Icons/Alerts/";

        /// <summary>
        /// Grid glyph on the corner button of a slot holding storage (shared with the storage panel). A simple shape:
        /// the button is 22 px, too small for the detailed open-box interaction icon.
        /// </summary>
        public const string OpenStorageIcon = "Assets/Art/Icons/Heroicons/Solid/ViewGrid.png";
    }
}
