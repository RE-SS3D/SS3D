using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.CodeGeneration
{
    public static class StaticClassCreator
    {
        #if UNITY_EDITOR
        /// <summary>
        /// Creates an static class in the object path, with the defined class name and using a list of provided assets as its elements.
        /// </summary>
        public static void CreateAtPath(string path, string className, [NotNull] IEnumerable<Object> assets, string namespaceName = "SS3D.Data.Enums")
        {
            IEnumerable<string> enums = assets.Select(reference => reference.name);

            string dataPath = Application.dataPath;
            string fullPath = dataPath + path;

            StaticClassWriter.WriteStaticClass(fullPath, className, enums, namespaceName);

            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.ShowNotification(new GUIContent($"All assets loaded and {namespaceName}.{className} class created at {path}."));
            }
        }
#endif
    }
}