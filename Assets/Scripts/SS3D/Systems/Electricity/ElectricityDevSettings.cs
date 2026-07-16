using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Development-only settings for visualizing power circuits and device connectivity.
    /// </summary>
    public static class ElectricityDevSettings
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool ShowElectricityGizmos { get; set; }
#else
        public static bool ShowElectricityGizmos => false;
#endif

        public static bool IsActive => ShowElectricityGizmos;

        public static Color GetColorForCircuit(int circuitIndex)
        {
            float hue = (circuitIndex * 0.381966011f) % 1f;
            Color color = Color.HSVToRGB(hue, 0.75f, 0.95f);
            color.a = 0.9f;
            return color;
        }

#if UNITY_EDITOR
        const string MenuPath = "SS3D/Dev/Electricity/Show Grid Gizmos";

        [UnityEditor.MenuItem(MenuPath, priority = 0)]
        static void ToggleShowElectricityGizmos()
        {
            ShowElectricityGizmos = !ShowElectricityGizmos;
            Debug.Log(
                ShowElectricityGizmos
                    ? "Electricity grid gizmos enabled. Scene view shows circuits, cable links, and device power stats."
                    : "Electricity grid gizmos disabled.");
        }

        [UnityEditor.MenuItem(MenuPath, true)]
        static bool ToggleShowElectricityGizmosValidate()
        {
            UnityEditor.Menu.SetChecked(MenuPath, ShowElectricityGizmos);
            return true;
        }
#endif
    }
}
