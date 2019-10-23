using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ss13_basic_tile))]
public class tile_builder_gui : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        ss13_basic_tile targetScript = (ss13_basic_tile)target;
        if(GUILayout.Button("init_tile"))
        {
            targetScript.initTile();
        }

    }
}
