using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coimbra;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SS3D.Data
{
    [CreateAssetMenu(menuName = "Database/Icons", fileName = "Icons")]
    public class  IconDatabase : ScriptableSettings
    {
        public string EnumName;
        public List<AssetReference> Assets;

        public void PreloadAssets()
        {
            foreach (AssetReference assetReference in Assets)
            {
                assetReference.LoadAssetAsync<Sprite>();
            }
        }

        public Sprite Get(InteractionIcons icon)
        {
            return Assets[(int)icon].Asset as Sprite;
        }

        public void CreateEnum()
        {
            IEnumerable<string> assets = Assets.Select(reference => reference.SubObjectName);

            CodeWriter.WriteEnum(GetAssetPath(), EnumName, assets);
        }

        public string GetAssetPath()
        {
            MonoScript ms = MonoScript.FromScriptableObject(this);
            string path = AssetDatabase.GetAssetPath(ms);

            string fullPath = Directory.GetParent(path)?.FullName;

            return fullPath;
        }
    }
}