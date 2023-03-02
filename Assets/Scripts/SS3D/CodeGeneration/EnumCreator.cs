using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        public static void CreateAtAssetPath(ScriptableObject assetPathSource, string enumName, IEnumerable<AssetReference> assets)
        {
            IEnumerable<string> enums = assets.Select(reference => reference.SubObjectName);

            CodeWriter.WriteEnum(GetAssetPath(assetPathSource), enumName, enums);
        }

        public static void CreateAtPath(string path, string enumName, IEnumerable<Object> assets, string namespaceName = "SS3D.Data.Enums")
        {
            IEnumerable<string> enums = assets.Select(reference => reference.name);

            string dataPath = Application.dataPath;
            string fullPath = dataPath + path;

            CodeWriter.WriteEnum(fullPath, enumName, enums, namespaceName);
            EditorWindow.focusedWindow.ShowNotification(new GUIContent($"{namespaceName}.{enumName} enum created at {path}."));
        }

        private static string GetAssetPath(ScriptableObject assetPathSource)
        {
            MonoScript ms = MonoScript.FromScriptableObject(assetPathSource);
            string path = AssetDatabase.GetAssetPath(ms);

            string fullPath = Directory.GetParent(path)?.FullName;

            return fullPath;
        }
#endif
    }
}