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
    [CustomEditor(typeof(WiresAdjacencyConnector))]
    public class AdjacencyEditor : UnityEditor.Editor
    {
        private Dictionary<string, SerializedObject> extraState;
        private bool[] blocked = new bool[Enum.GetValues(typeof(Direction)).Length];

        public override void OnInspectorGUI()
        {
            WiresAdjacencyConnector connector = (WiresAdjacencyConnector)target;
            //var connectorSerial = connector.GetComponentInChildren(typeof(TileStateCommunicator));
            var subTiles = connector.GetComponentsInChildren(typeof(TileStateCommunicator));
            if (extraState == null || subTiles.Length != extraState.Count || subTiles.Any(subTile => !extraState.ContainsKey(subTile.gameObject.name)))
            {
                extraState = subTiles.Select(state => new Tuple<string, SerializedObject>(state.gameObject.name, new SerializedObject(state))).ToDictionary(x => x.Item1, x => x.Item2);
            }

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
                    //foreach (var state in extraState)
                    //{
                    //    state.Value.Update();
                    //    SerializedProperty property = state.Value.FindProperty("tileState");
                    //    state.Value.ApplyModifiedProperties();
                    //}

                    bool modified = false;
                    var connectorSerial = new SerializedObject(connector);
                    connectorSerial.Update();

                    SerializedProperty property = connectorSerial.FindProperty("tileState");
                    property.FindPropertyRelative("blockedDirection").intValue = SetBitmap(blocked);

                    //var stateNow = connector.TileState;

                    //stateNow.blockedDirection = SetBitmap(blocked);
                    //connector.SetTileState(stateNow);

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