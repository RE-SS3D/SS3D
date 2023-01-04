using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coimbra;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SS3D.Data
{
    public static class AssetData
    {
        private static InteractionIconsDatabase _interactionIcons;

        public static Sprite Get(InteractionIcons icon) => _interactionIcons.Get(icon);

        public static void InitializeAssets()
        {
            InitializeInteractionIcons();
        }

        public static void InitializeInteractionIcons()
        {
            ScriptableSettings.TryGet(out InteractionIconsDatabase icons);

            _interactionIcons = icons;
            _interactionIcons.PreloadAssets();
        }

#if UNITY_EDITOR
        public static void CreateEnum(ScriptableObject assetPathSource, string enumName, List<AssetReference> assets)
        {
            IEnumerable<string> enums = assets.Select(reference => reference.SubObjectName);

            CodeWriter.WriteEnum(GetAssetPath(assetPathSource), enumName, enums);
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