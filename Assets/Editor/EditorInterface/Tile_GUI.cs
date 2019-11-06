using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mirror{
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
            targetScript.GetComponent<TilePipeManager>().BuildDisposal(-1);
        }

        if(GUILayout.Button("delete_disposal"))
        {
            targetScript.GetComponent<TilePipeManager>().DeleteDisposal();
        }

        if(GUILayout.Button("update_disposal"))
        {
            targetScript.GetComponent<TilePipeManager>().UpdateDisposal(true);
        }
    
        if(GUILayout.Button("update_blueModel"))
        {
            targetScript.GetComponent<TilePipeManager>().UpdateBlueModel();
        }

        if(GUILayout.Button("UPDATE"))
        {
            targetScript.GetComponent<Tile>().UpdateTile();
        }
    }
}
}
