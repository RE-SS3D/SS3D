using System;
using System.Reflection;
using UnityEditor;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Puts the Game view at the pilots' 1920x1080 and back. Unity has no public Game View size API, so the
    /// version-sensitive reflection lives here, fails explicitly if it changes, and restores the user's selected size.
    /// </summary>
    public static class AutomationGameView
    {
        private const string Label = "SS3D UI Pilot 1920x1080";

        /// <summary>Selects the 1920x1080 size (adding it once) and returns the size index it replaced.</summary>
        public static int Select1080p()
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance").GetValue(null);
            object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
            Type groupType = group.GetType();
            int builtIn = (int)groupType.GetMethod("GetBuiltinCount").Invoke(group, null);
            int custom = (int)groupType.GetMethod("GetCustomCount").Invoke(group, null);
            int selected = -1;
            for (int i = 0; i < builtIn + custom; i++)
            {
                object size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((string)size.GetType().GetProperty("baseText").GetValue(size) == Label) selected = i;
            }
            if (selected < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
                var kindType = assembly.GetType("UnityEditor.GameViewSizeType", true);
                object size = Activator.CreateInstance(sizeType, Enum.ToObject(kindType, 1), 1920, 1080, Label);
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                selected = builtIn + custom;
            }
            EditorWindow view = GameView();
            int previous = (int)SizeProperty().GetValue(view);
            SizeProperty().SetValue(view, selected);
            view.Focus();
            return previous;
        }

        public static void Restore(int index) => SizeProperty().SetValue(GameView(), index);

        private static EditorWindow GameView() => EditorWindow.GetWindow(
            typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView", true));

        private static PropertyInfo SizeProperty() => GameView().GetType().GetProperty(
            "selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
