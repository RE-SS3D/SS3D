using SS3D.Engine.Tiles.Connections;
using System;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Tiles.Editor.TileMapEditor
{
    /// <summary>
    /// Custom editor used for setting blocked connections.
    /// </summary>
    [CustomEditor(typeof(MultiAdjacencyConnector))]
    public class AdjacencyEditor : UnityEditor.Editor
    {
        private bool[] blocked = new bool[8];
        private bool showAdjacencyOptions = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Serialize the object as this is the prefered way to change objects in the editor
            MultiAdjacencyConnector connector = (MultiAdjacencyConnector)target;
            SerializedObject serializedConnector = new SerializedObject(connector);
            SerializedProperty property = serializedConnector.FindProperty("EditorblockedConnections");
            blocked = ParseBitmap((byte)property.intValue);


            showAdjacencyOptions = EditorGUILayout.BeginFoldoutHeaderGroup(showAdjacencyOptions, "Blocked connections");
            if (showAdjacencyOptions)
            {
                EditorGUILayout.BeginVertical();

                // Top line
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20));
                blocked[7] = EditorGUILayout.Toggle(blocked[7]);
                blocked[0] = EditorGUILayout.Toggle(blocked[0]);
                blocked[1] = EditorGUILayout.Toggle(blocked[1]);
                EditorGUILayout.EndHorizontal();

                // Middle line
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20));
                blocked[6] = EditorGUILayout.Toggle(blocked[6]);
                EditorGUILayout.Space(14);
                blocked[2] = EditorGUILayout.Toggle(blocked[2]);
                EditorGUILayout.EndHorizontal();

                // Last line
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20));
                blocked[5] = EditorGUILayout.Toggle(blocked[5]);
                blocked[4] = EditorGUILayout.Toggle(blocked[4]);
                blocked[3] = EditorGUILayout.Toggle(blocked[3]);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


            if (GUI.changed && connector != null)
            {
                bool changed = false;
                serializedConnector.Update();
                property.intValue = SetBitmap(blocked);

                changed = serializedConnector.ApplyModifiedProperties();

                if (changed)
                {
                    // Get the PlacedTileObject and map
                    var placedObject = connector.gameObject.GetComponent<PlacedTileObject>();
                    TileMap map = connector.gameObject.GetComponentInParent<TileMap>();

                    // Get all neighbours
                    var neighbourObjects = map.GetNeighbourObjects(placedObject.GetLayer(), 0, placedObject.transform.position);
                    
                    for (int i = 0; i < neighbourObjects.Length; i++)
                    {
                        MultiAdjacencyConnector adjacencyNeighbour = neighbourObjects[i]?.gameObject.GetComponent<MultiAdjacencyConnector>();
                        if (adjacencyNeighbour)
                        {
                            // Serialize their object
                            SerializedObject serializedNeighbourConnector = new SerializedObject(adjacencyNeighbour);
                            serializedNeighbourConnector.Update();
                            SerializedProperty neighbourProperty = serializedNeighbourConnector.FindProperty("EditorblockedConnections");

                            // Set their opposite side blocked
                            neighbourProperty.intValue = AdjacencyBitmap.SetDirection((byte)neighbourProperty.intValue, TileHelper.GetOpposite((Direction)i), blocked[i]);

                            // Apply the changes
                            serializedNeighbourConnector.ApplyModifiedProperties();
                            adjacencyNeighbour.UpdateBlockedFromEditor();
                            adjacencyNeighbour.UpdateSingle(TileHelper.GetOpposite((Direction)i), placedObject);
                        }
                    }

                    // Set their adjacency connector
                    connector.UpdateBlockedFromEditor();
                    placedObject.UpdateAllAdjacencies(neighbourObjects);
                }
            }
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