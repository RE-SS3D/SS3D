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

        if(GUILayout.Button("build_disposal"))
        {
            targetScript.GetComponent<TilePipeManager>().BuildDisposal();
        }

        if(GUILayout.Button("delete_disposal"))
        {
            targetScript.GetComponent<TilePipeManager>().DeleteDisposal();
        }

        if(GUILayout.Button("update_disposal"))
        {
            targetScript.GetComponent<TilePipeManager>().UpdateDisposal();
        }

    }
}

