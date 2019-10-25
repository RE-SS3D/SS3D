using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Tile))]
public class Tile_GUI : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        Tile targetScript = (Tile)target;
        if(GUILayout.Button("init_tile"))
        {
            targetScript.initTile();
        }

    }
}

