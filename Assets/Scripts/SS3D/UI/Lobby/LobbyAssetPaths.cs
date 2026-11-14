namespace SS3D.UI.Lobby
{
    /// <summary>
    /// Stable asset paths for the pre-round lobby UITK shell.
    /// Rebuild via <c>SS3D → Data → Rebuild All UI Catalogs</c>.
    /// </summary>
    public static class LobbyAssetPaths
    {
        public const string ResourcesCatalogName = "LobbyAssetCatalog";
        public const string CatalogAssetPath =
            "Assets/Content/Systems/UI/Lobby/Resources/LobbyAssetCatalog.asset";

        public const string StyleSheet = "Assets/Content/Systems/UI/Lobby/LobbyShell.uss";

        /// <summary>The lobby shell; it links <c>Lobby.uss</c> itself.</summary>
        public const string Template = "Assets/Content/Systems/UI/Lobby/Lobby.uxml";

        /// <summary>The character window; it links <c>CharacterWindow.uss</c> itself.</summary>
        public const string CharacterWindowTemplate = "Assets/Content/Systems/UI/Lobby/CharacterWindow.uxml";

        public const string IconRoot = "Assets/Art/Icons/Lobby/";
        public const string JobIconRoot = IconRoot + "Jobs/";

        /// <summary>Baked style thumbnails for the character window's grids (hair-*, beard-*, eyebrows-*).</summary>
        public const string StyleThumbnailRoot = IconRoot + "Styles/";
        public const string PreviewPlaceholder = IconRoot + "PnSecurity.png";
        public const string LoadoutJanitor = IconRoot + "PnJanitor.png";
        public const string PreviewHumanPrefab =
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
        public const string ServerInfoBanner =
            "Assets/Art/Textures/Lobby/ProbablyNotChaseBanner.png";
        public const string HeroiconsOutlineRoot = "Assets/Art/Icons/Heroicons/Outline/";
        public const string ChevronDownIcon = HeroiconsOutlineRoot + "ChevronDown.png";
    }
}
