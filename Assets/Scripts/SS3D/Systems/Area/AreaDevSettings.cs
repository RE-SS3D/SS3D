using UnityEngine;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// Development-only settings for visualizing APC flood-filled areas.
    /// </summary>
    public static class AreaDevSettings
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool ShowAreaGizmos { get; set; }
#else
        public static bool ShowAreaGizmos => false;
#endif

        public static bool IsActive => ShowAreaGizmos;

        public static Color GetColorForArea(ushort areaId)
        {
            if (areaId == AreaId.None)
            {
                return Color.clear;
            }

            float hue = (areaId * 0.6180339887f) % 1f;
            Color color = Color.HSVToRGB(hue, 0.62f, 0.95f);
            color.a = 0.55f;
            return color;
        }

#if UNITY_EDITOR
        const string MenuPath = "SS3D/Dev/Areas/Show Area Gizmos";

        [UnityEditor.MenuItem(MenuPath, priority = 0)]
        static void ToggleShowAreaGizmos()
        {
            ShowAreaGizmos = !ShowAreaGizmos;
            Debug.Log(
                ShowAreaGizmos
                    ? "Area gizmos enabled. Open the Scene view with Gizmos on while playing as host/server."
                    : "Area gizmos disabled.");
        }

        [UnityEditor.MenuItem(MenuPath, true)]
        static bool ToggleShowAreaGizmosValidate()
        {
            UnityEditor.Menu.SetChecked(MenuPath, ShowAreaGizmos);
            return true;
        }
#endif
    }
}
