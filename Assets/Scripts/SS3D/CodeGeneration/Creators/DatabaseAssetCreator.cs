using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.CodeGeneration
{
    public static class DatabaseAssetCreator
    {
         #if UNITY_EDITOR
        /// <summary>
        /// Creates an enum in the object path, with the defined enum name and using a list of provided assets as its elements.
        /// </summary>
        /// <param name="assetPathSource"></param>
        /// <param name="enumName"></param>
        /// <param name="assets"></param>
        public static void CreateAtPath(string path, Type classType, string className, IEnumerable<Object> assets, string namespaceName = "SS3D.Data.Enums")
        {
            IEnumerable<string> enums = assets.Select(reference => reference.name);

            string dataPath = Application.dataPath;
            string fullPath = dataPath + path;

            DatabaseAssetWriter.Write(fullPath, classType, className, enums, namespaceName);

            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.ShowNotification(new GUIContent($"All assets loaded and {namespaceName}.{className} class created at {path}."));
            }
        }
#endif
    }
}