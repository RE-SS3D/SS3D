using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.CodeGeneration.Creators
{
    public static class DatabaseAssetCreator
    {
         #if UNITY_EDITOR
        /// <summary>
        /// Creates a list of database assets in the object path, with the defined class name and using a list of provided assets as its elements.
        /// </summary>
        public static void CreateAtPath(string path, Type classType, string className, [NotNull] IEnumerable<Object> assets, string namespaceName = "SS3D.Data.Generated")
        {
            IEnumerable<string> enums = assets.Select(reference => reference.name);

            string dataPath = Application.dataPath;
            string fullPath = dataPath + path;

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            DatabaseAssetWriter.Write(fullPath, classType, className, enums, namespaceName);

            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.ShowNotification(new GUIContent($"All assets loaded and {namespaceName}.{className} class created at {path}."));
            }
        }
#endif
    }
}