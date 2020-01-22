using UnityEngine;
using System.Collections;
using TileMap;
using System.Collections.Generic;
using UnityEditor;

namespace TileMap.Editor {

    /**
     * Used purely in TileMapEditor for arguments/settings. Stored as asset for reuse.
     */
    public class TileMapEditorArgs : ScriptableObject
    {
        public List<ConstructibleTile> tiles;

        public static TileMapEditorArgs LoadFromAsset()
        {
            TileMapEditorArgs args = AssetDatabase.LoadAssetAtPath<TileMapEditorArgs>("Assets/Editor/TileMapSettings.asset");
            if (args == null) {
                args = CreateInstance<TileMapEditorArgs>();
                AssetDatabase.CreateAsset(args, "Assets/Editor/TileMapSettings.asset");
                args.tiles = new List<ConstructibleTile>();
            }
            return args;
        }
    }
}