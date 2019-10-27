using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Tile_loader))]
public class Tile_loader_GUI : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        Tile_loader targetScript = (Tile_loader)target;
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
