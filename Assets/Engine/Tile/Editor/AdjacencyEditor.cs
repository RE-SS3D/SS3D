using SS3D.Engine.Tiles.Connections;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Tiles.Editor
{
    [CustomEditor(typeof(WiresAdjacencyConnector))]
    public class AdjacencyEditor : UnityEditor.Editor
    {
        private bool[] blocked = new bool[Enum.GetValues(typeof(Direction)).Length];

        public override void OnInspectorGUI()
        {
            WiresAdjacencyConnector connector = (WiresAdjacencyConnector)target;
            TileObject tileObject = connector.GetComponentInParent<TileObject>();
            blocked = connector.Blocked;

            serializedObject.Update();
            EditorGUILayout.PrefixLabel("Blocked direction");

            // Top line
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20));
            EditorGUILayout.Space(15);
            blocked[0] = EditorGUILayout.Toggle(blocked[0]);

            //for (int i = 0;  i < 3; i++)
            //{
            //    blocked[i] = EditorGUILayout.Toggle(blocked[i]);
            //}

            EditorGUILayout.EndHorizontal();


            // Middle line
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20));
            blocked[6] = EditorGUILayout.Toggle(blocked[6]);
            EditorGUILayout.Space(15);
            blocked[2] = EditorGUILayout.Toggle(blocked[2]);
            EditorGUILayout.EndHorizontal();

            // Last line
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20));
            EditorGUILayout.Space(15);
            blocked[4] = EditorGUILayout.Toggle(blocked[4]);

            //for (int i = 5; i < 8; i++)
            //{
            //    blocked[i] = EditorGUILayout.Toggle(blocked[i]);
            //}

            EditorGUILayout.EndHorizontal();


            serializedObject.ApplyModifiedProperties();

            connector.Blocked = blocked;

            if (GUI.changed)
            {
                if (tileObject != null)
                {
                    tileObject.RefreshAdjacencies();
                    //tileObject.UpdateAllAdjacencies(new[] { tileObject.Tile });
                }
                else
                    Debug.LogWarning("No tileobject found by adjacency editor");
            }
        }
    }
}