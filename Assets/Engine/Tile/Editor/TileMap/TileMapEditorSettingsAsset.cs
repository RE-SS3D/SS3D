using UnityEngine;
using System.Collections;
using SS3D.Engine.Tiles;
using System.Collections.Generic;
using UnityEditor;

namespace SS3D.Engine.Tiles.Editor {

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