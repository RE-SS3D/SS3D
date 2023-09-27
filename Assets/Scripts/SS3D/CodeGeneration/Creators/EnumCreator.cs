using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.CodeGeneration
{
    public static class EnumCreator
    {
#if UNITY_EDITOR
        /// <summary>
        /// Creates an enum in the object path, with the defined enum name and using a list of provided assets as its elements.
        /// </summary>
        /// <param name="assetPathSource"></param>
        /// <param name="enumName"></param>
        /// <param name="assets"></param>
        public static void CreateAtPath(string path, string enumName, IEnumerable<Object> assets, string namespaceName = "SS3D.Data.Enums")
        {
            IEnumerable<string> enums = assets.Select(reference => reference.name);

            List<string> enumerable = enums.ToList();
            enumerable.Insert(0, "None");

            string dataPath = Application.dataPath;
            string fullPath = dataPath + path;

            CodeWriter.WriteEnum(fullPath, enumName, enumerable, namespaceName);
            EditorWindow.focusedWindow.ShowNotification(new GUIContent($"All assets loaded and {namespaceName}.{enumName} enum created at {path}."));
        }
#endif
    }
}