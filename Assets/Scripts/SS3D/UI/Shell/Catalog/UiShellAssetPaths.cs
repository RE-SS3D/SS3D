namespace SS3D.UI.Shell.Catalog
{
    /// <summary>
    /// Stable asset paths for the <see cref="UiShellSubSystem"/> catalog. Rebuild the committed
    /// catalog with <c>SS3D → Data → Rebuild All UI Catalogs</c>.
    /// </summary>
    public static class UiShellAssetPaths
    {
        public const string ResourcesCatalogName = "UiShellAssetCatalog";

        public const string CatalogAssetPath =
            "Assets/Content/Systems/UI/Shell/Resources/UiShellAssetCatalog.asset";

        public const string RebuildMenuPath = "SS3D → Data → Rebuild All UI Catalogs";

        // Reuses the existing HUD overlay panel settings (radial menu / armed overlay already render
        // through it) rather than minting a fourth PanelSettings asset for the same screen-space overlay.
        public const string PanelSettings =
            "Assets/Content/Systems/UI/Interactions/RadialInteractionMenu/HudOverlayPanelSettings.asset";

        public const string Ss3dTokens =
            "Assets/Content/Systems/UI/Tokens/ss3d-tokens.uss";

        public const string Ss3dTypography =
            "Assets/Content/Systems/UI/Tokens/ss3d-typography.uss";
    }
}
