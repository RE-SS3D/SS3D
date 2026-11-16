using UnityEngine;

namespace SS3D.Systems.IdAccess
{
    /// <summary>
    /// Development-only toggle for ID console access editing.
    /// Release builds always require Change ID clearance.
    /// </summary>
    public static class IdAccessDevSettings
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool AllowConsoleAccessEditing { get; set; } = true;
#else
        public static bool AllowConsoleAccessEditing => false;
#endif

        public static bool IsActive => AllowConsoleAccessEditing;

#if UNITY_EDITOR
        const string MenuPath = "SS3D/Dev/Id Access/Allow Console Access Editing";

        [UnityEditor.MenuItem(MenuPath, priority = 0)]
        static void ToggleAllowConsoleAccessEditing()
        {
            AllowConsoleAccessEditing = !AllowConsoleAccessEditing;
            Debug.Log(
                AllowConsoleAccessEditing
                    ? "ID console access editing enabled without Change ID (dev only)."
                    : "ID console access editing now requires Change ID clearance.");
        }

        [UnityEditor.MenuItem(MenuPath, true)]
        static bool ToggleAllowConsoleAccessEditingValidate()
        {
            UnityEditor.Menu.SetChecked(MenuPath, AllowConsoleAccessEditing);
            return true;
        }
#endif
    }
}
