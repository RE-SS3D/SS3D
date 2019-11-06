using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mirror{
[CustomEditor(typeof(TileContentManager))]
public class Tile_Content_Manager_GUI : Editor
{

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        TileContentManager targetScript = (TileContentManager)target;
        if(GUILayout.Button("init_manager"))
        {
            targetScript.InitTileContentManager();
        }
        if(GUILayout.Button("test_build"))
        {
            targetScript.BuildContent_target(0);
        }
        if(GUILayout.Button("del_build"))
        {
            targetScript.DeleteContents();
        }
        if(GUILayout.Button("update_connectable"))
        {
            targetScript.UpdateMultipart();
        }
        if(GUILayout.Button("update_neighbours"))
        {
            targetScript.UpdateNeighbours();
        }
    

    }
}
}