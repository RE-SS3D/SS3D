namespace System.Electricity
{
    /// <summary>
    /// Development-only toggle so wall light fixtures can glow without a powered grid.
    /// Release builds always require real power.
    /// </summary>
    public static class LightingDevBypass
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool AlwaysPowerLightFixtures { get; set; } = true;
#else
        public static bool AlwaysPowerLightFixtures => false;
#endif

        public static bool IsActive => AlwaysPowerLightFixtures;

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
                foreach (var fixture in UnityEngine.Object.FindObjectsByType<LightPower>(UnityEngine.FindObjectsSortMode.None))
                {
                    fixture.RefreshVisuals();
                }
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
