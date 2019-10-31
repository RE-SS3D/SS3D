using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mirror{
[CustomEditor(typeof(Turf))]
public class Turf_GUI : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        Turf targetScript = (Turf)target;
        if(GUILayout.Button("build_upper_turf"))
        {
            targetScript.BuildUpper();
        }
        if(GUILayout.Button("build_lower_turf"))
        {
            targetScript.BuildLower();
        }
        if(GUILayout.Button("build_turf"))
        {
            targetScript.BuildTurf();
        }
        if(GUILayout.Button("update_turf"))
        {
            targetScript.UpdateTurf();
        }
        if(GUILayout.Button("update_neighbour_turf"))
        {
            targetScript.UpdateNeighbourTurf();
        }

    }
}
}