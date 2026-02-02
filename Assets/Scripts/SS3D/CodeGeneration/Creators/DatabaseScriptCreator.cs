using JetBrains.Annotations;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.CodeGeneration.Creators
{
    public static class DatabaseScriptCreator
    {
         #if UNITY_EDITOR
        /// <summary>
        /// Creates a list of database assets in the object path, with the defined class name and using a list of provided assets as its elements.
        /// </summary>
        public static void CreateAtPath(string path, string className, [NotNull] List<Object> assets, string namespaceName = "SS3D.Data.Generated")
        {
            string dataPath = Application.dataPath;
            string fullPath = dataPath + path;

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            DatabaseScriptWriter.Write(fullPath, className, assets, namespaceName);

            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.ShowNotification(new GUIContent($"All assets loaded and {namespaceName}.{className} class created at {path}."));
            }
        }
#endif
    }
}