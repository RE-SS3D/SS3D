#if UNITY_EDITOR
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.TileMap
{
    /// <summary>
    /// Custom editor used for setting blocked connections.
    /// </summary>
    [CustomEditor(typeof(MultiAdjacencyConnector))]
    public class AdjacencyEditor : UnityEditor.Editor
    {
        //TODO: This can be refactored to be Dictionary<Direction, bool>, which would eliminate the need for conversions when working with AdjacencyMap
        private bool[] _blocked = new bool[8];
        private bool _showAdjacencyOptions = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Serialize the object as this is the prefered way to change objects in the editor
            MultiAdjacencyConnector connector = (MultiAdjacencyConnector)target;
            SerializedObject serializedConnector = new(connector);
            SerializedProperty property = serializedConnector.FindProperty("EditorBlockedConnections");
            _blocked = ParseBitmap((byte)property.intValue);


            _showAdjacencyOptions = EditorGUILayout.BeginFoldoutHeaderGroup(_showAdjacencyOptions, "Blocked connections");
            if (_showAdjacencyOptions)
            {
                EditorGUILayout.BeginVertical();

                // Top line
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20));
                _blocked[7] = EditorGUILayout.Toggle(_blocked[7]);
                _blocked[0] = EditorGUILayout.Toggle(_blocked[0]);
                _blocked[1] = EditorGUILayout.Toggle(_blocked[1]);
                EditorGUILayout.EndHorizontal();

                // Middle line
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20));
                _blocked[6] = EditorGUILayout.Toggle(_blocked[6]);
                EditorGUILayout.Space(14);
                _blocked[2] = EditorGUILayout.Toggle(_blocked[2]);
                EditorGUILayout.EndHorizontal();

                // Last line
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20));
                _blocked[5] = EditorGUILayout.Toggle(_blocked[5]);
                _blocked[4] = EditorGUILayout.Toggle(_blocked[4]);
                _blocked[3] = EditorGUILayout.Toggle(_blocked[3]);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();


            if (!GUI.changed || connector == null)
            {
                return;
            }

            serializedConnector.Update();
            property.intValue = SetBitmap(_blocked);

            bool changed = serializedConnector.ApplyModifiedProperties();

            if (!changed)
            {
                return;
            }

            // Get the PlacedTileObject and map
            PlacedTileObject placedObject = connector.gameObject.GetComponent<PlacedTileObject>();
            SS3D.Systems.Tile.TileMap map = connector.gameObject.GetComponentInParent<SS3D.Systems.Tile.TileMap>();

            // Get all neighbours
            PlacedTileObject[] neighbourObjects = map.GetNeighbourObjects(placedObject.GetLayer(), 0, placedObject.transform.position);

            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                MultiAdjacencyConnector adjacencyNeighbour = neighbourObjects[i]?.gameObject.GetComponent<MultiAdjacencyConnector>();
                if (!adjacencyNeighbour)
                {
                    continue;
                }

                // Serialize their object
                SerializedObject serializedNeighbourConnector = new(adjacencyNeighbour);
                serializedNeighbourConnector.Update();
                SerializedProperty neighbourProperty = serializedNeighbourConnector.FindProperty("EditorBlockedConnections");

                // Set their opposite side blocked
                AdjacencyMap adjacencyMap = new();
                AdjacencyData[] adjacencyData = AdjacencyMap.DeserializeFromByte((byte)neighbourProperty.intValue);
                adjacencyMap.Connections = adjacencyData;

                adjacencyMap.SetConnection(TileHelper.GetOpposite((Direction) i), new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, _blocked[i]));
                neighbourProperty.intValue = adjacencyMap.SerializeToByte();

                // Apply the changes
                serializedNeighbourConnector.ApplyModifiedProperties();
                adjacencyNeighbour.UpdateBlockedFromEditor();
                adjacencyNeighbour.UpdateSingle(TileHelper.GetOpposite((Direction)i), placedObject);
            }

            // Set their adjacency connector
            connector.UpdateBlockedFromEditor();
            placedObject.UpdateAllAdjacencies(neighbourObjects);
        }

        private bool[] ParseBitmap(byte bitmap)
        {
            bool[] result = new bool[8];

            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                result[(int)direction] = ((bitmap >> (int) direction) & 0x1) != 0;
            }

            return result;
        }

        private byte SetBitmap(bool[] items)
        {
            AdjacencyMap adjacencyMap = new();
            for (Direction direction = Direction.North; direction <= Direction.NorthWest; direction++)
            {
                adjacencyMap.SetConnection(direction, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, _blocked[(int)direction]));
            }

            return adjacencyMap.SerializeToByte();
        }
    }
}
#endif