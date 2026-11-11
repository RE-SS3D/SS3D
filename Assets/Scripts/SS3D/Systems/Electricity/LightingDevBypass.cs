namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Development-only toggle so wall light fixtures can glow without a powered grid.
    /// When active, fixtures are treated as powered without grid supply. Fixtures with an area
    /// still follow APC channel toggles and Normal/Emergency/Dark policy; fixtures with no area
    /// (manual place / no APC flood) glow. Release builds require real power unless the map editor
    /// "Always Power Lights" setting is on.
    /// </summary>
    public static class LightingDevBypass
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool AlwaysPowerLightFixtures { get; set; }
#else
        public static bool AlwaysPowerLightFixtures => false;
#endif

        /// <summary>
        /// Map editor "Always Power Lights" setting (server-side, available in all builds so testers can
        /// author lighting without wiring a grid). Admin-gated by <c>MapEditorPermissions</c>.
        /// </summary>
        public static bool MapEditorOverride { get; set; }

        public static bool IsActive => AlwaysPowerLightFixtures || MapEditorOverride;

#if UNITY_EDITOR
        const string MenuPath = "SS3D/Dev/Lighting/Always Power Light Fixtures";

        [UnityEditor.MenuItem(MenuPath, priority = 0)]
        static void ToggleAlwaysPowerLightFixtures()
        {
            AlwaysPowerLightFixtures = !AlwaysPowerLightFixtures;
            UnityEngine.Debug.Log(
                AlwaysPowerLightFixtures
                    ? "Light fixtures will stay on without electricity (dev only)."
                    : "Light fixtures now require electricity.");

            if (UnityEngine.Application.isPlaying)
            {
                LightPower.RefreshAllFixtures();
            }
        }

        [UnityEditor.MenuItem(MenuPath, true)]
        static bool ToggleAlwaysPowerLightFixturesValidate()
        {
            UnityEditor.Menu.SetChecked(MenuPath, AlwaysPowerLightFixtures);
            return true;
        }
#endif
    }
}
