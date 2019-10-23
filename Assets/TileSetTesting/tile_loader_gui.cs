using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ss13_tile_loader))]
public class tile_loader_gui : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        ss13_tile_loader targetScript = (ss13_tile_loader)target;
        if(GUILayout.Button("load_map"))
        {
            targetScript.GenerateLevel();
        }
        if(GUILayout.Button("del_map"))
        {
            targetScript.DeleteLevel();
        }
    }
}
