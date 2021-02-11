using SS3D.Engine.Tiles.Connections;
using SS3D.Engine.Tiles.State;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Tiles.Editor
{
    [CustomEditor(typeof(AdjacencyStateMaintainer), true)]
    public class AdjacencyEditor : UnityEditor.Editor
    {
        private Dictionary<string, SerializedObject> extraState;
        private bool[] blocked = new bool[Enum.GetValues(typeof(Direction)).Length];

        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            AdjacencyStateMaintainer connector = (AdjacencyStateMaintainer)target;
            TileObject tileObject = connector.GetComponentInParent<TileObject>();
            

            blocked = ParseBitmap(connector.TileState.blockedDirection);

            serializedObject.Update();
            EditorGUILayout.PrefixLabel("Blocked direction");

            // Top line
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20));
            EditorGUILayout.Space(15);
            blocked[0] = EditorGUILayout.Toggle(blocked[0]);
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
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
            {
                if (tileObject != null)
                {

                    bool modified = false;
                    var connectorSerial = new SerializedObject(connector);
                    connectorSerial.Update();

                    SerializedProperty property = connectorSerial.FindProperty("tileState");
                    property.FindPropertyRelative("blockedDirection").intValue = SetBitmap(blocked);

                    modified = connectorSerial.ApplyModifiedProperties();
                    if (modified)
                    {
                        tileObject.RefreshSubData();
                        tileObject.RefreshAdjacencies();
                    }
                }
                else
                    Debug.LogWarning("No tileobject found by adjacency editor");
            }

            serializedObject.ApplyModifiedProperties();
        }


        private bool[] ParseBitmap(byte bitmap)
        {
            bool[] result = new bool[8];

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                result[(int)direction] = AdjacencyBitmap.Adjacent(bitmap, direction) != 0;
            }

            return result;
        }

        private byte SetBitmap(bool[] items)
        {
            byte result = new byte();

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                result = AdjacencyBitmap.SetDirection(result, direction, blocked[(int)direction]);
            }

            return result;
        }
    }
}