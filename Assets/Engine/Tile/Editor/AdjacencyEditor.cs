using SS3D.Engine.Tiles.Connections;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Tiles.Editor
{
    [CustomEditor(typeof(WiresAdjacencyConnector))]
    public class AdjacencyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            WiresAdjacencyConnector connector = (WiresAdjacencyConnector)target;


        }
    }
}