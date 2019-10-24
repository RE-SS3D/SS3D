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
        if(GUILayout.Button("test_build"))
        {
            targetScript.buildContent(0);
        }
        if(GUILayout.Button("del_build"))
        {
            targetScript.DeleteContents();
        }

    }
}

