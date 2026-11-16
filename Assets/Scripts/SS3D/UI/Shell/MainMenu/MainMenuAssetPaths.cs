namespace SS3D.UI.Shell.MainMenu
{
    public static class MainMenuAssetPaths
    {
        public const string Catalog = "Assets/Content/Systems/UI/MainMenu/Resources/MainMenuAssetCatalog.asset";
        public const string Template = "Assets/Content/Systems/UI/MainMenu/MainMenu.uxml";
        public const string Style = "Assets/Content/Systems/UI/MainMenu/MainMenu.uss";
        public const string Logo = "Assets/Art/Textures/UI/Logos/SS3DLogo.png";
        /// <summary>Every image here joins the wallpaper pick: 1920×1080, no transparency.</summary>
        public const string WallpaperFolder = "Assets/Art/Textures/UI/Wallpapers";
        /// <summary>Every clip here joins the wallpaper pick: VP8 .webm, silent, loops cleanly.</summary>
        public const string WallpaperVideoFolder = "Assets/Art/Videos/Wallpapers";
        public const string MusicIcon = "Assets/Art/Icons/Heroicons/Solid/MusicNote.png";
        /// <summary>Every clip here plays in the menu except the intro stings.</summary>
        public const string MusicFolder = "Assets/Art/Sound/World/Music";
        public static readonly string[] ExcludedMusic = { "Intro", "IntroSneeze" };
        public const string ResourcesCatalogName = "MainMenuAssetCatalog";
    }
}
