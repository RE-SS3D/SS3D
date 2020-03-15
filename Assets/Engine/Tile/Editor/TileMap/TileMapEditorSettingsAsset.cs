using UnityEngine;
using System.Collections;
using Engine.Tiles;
using System.Collections.Generic;
using UnityEditor;

namespace Engine.Tiles.Editor {

    /**
     * This object is used to save the settings defined in the tilemap, so they may be retrieved later.
     */
    public class TileMapEditorSettingsAsset : ScriptableObject
    {
        public List<TileDefinition> tiles;

        public static TileMapEditorSettingsAsset LoadFromAsset()
        {
            TileMapEditorSettingsAsset args = AssetDatabase.LoadAssetAtPath<TileMapEditorSettingsAsset>("Assets/Editor/TileMapSettings.asset");
            if (args == null) {
                args = CreateInstance<TileMapEditorSettingsAsset>();
                AssetDatabase.CreateAsset(args, "Assets/Editor/TileMapSettings.asset");
                args.tiles = new List<TileDefinition>();
            }
            return args;
        }
    }
}