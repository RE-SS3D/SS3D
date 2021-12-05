using UnityEngine;
using System;
using UnityEditor;
using SS3D.Engine.Tiles;
using SS3D.Engine.Tiles.Connections;

namespace SS3D.Content.Structures.Fixtures
{
    public class Door : MonoBehaviour, IAdjacencyConnector
    {
        public enum DoorType
        {
            Single,
            Double
        };

        /** <summary>Based on peculiarities of the model, the appropriate position of the wall cap</summary> */
        private const float WALL_CAP_DISTANCE_FROM_CENTRE = -1.0f;

        // As is the standard in the rest of the code, wallCap should face east.
        [SerializeField]
        private GameObject wallCapPrefab = null;

        [SerializeField]
        private DoorType doorType;

        private Direction doorDirection;
        private TileMap map;

        private void OnEnable()
        {
            // Note: 'Should' already be validated by the point the game starts.
            // So the only purpose is when loading map from scene to correctly load children.
            ValidateChildren();

            doorDirection = GetComponent<PlacedTileObject>().GetDirection();
        }

        public void CleanAdjacencies()
        {
            if (!map)
                map = GetComponentInParent<TileMap>();

            SetPerpendicularBlocked(false);

            var neighbourObjects = map.GetNeighbourObjects(TileLayer.Turf, 0, transform.position);
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                neighbourObjects[i]?.UpdateSingleAdjacency(TileHelper.GetOpposite((Direction)i), null);
            }
        }

        public void UpdateSingle(Direction direction, PlacedTileObject placedObject)
        {

            if (UpdateSingleConnection(direction, placedObject))
                UpdateWallCaps();
        }

        public void UpdateAll(PlacedTileObject[] neighbourObjects)
        {
            // Because we are on a Furniture layer and walls are on the Turf. Discard furniture neighbours and get the turf neighbours.
            if (!map)
                map = GetComponentInParent<TileMap>();

            neighbourObjects = map.GetNeighbourObjects(TileLayer.Turf, 0, transform.position);
            PlacedTileObject currentObject = GetComponent<PlacedTileObject>();

            bool changed = false;
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                bool updatedSingle = false;
                updatedSingle = UpdateSingleConnection((Direction)i, neighbourObjects[i]);
                if (updatedSingle && neighbourObjects[i])
                    neighbourObjects[i].UpdateSingleAdjacency(TileHelper.GetOpposite((Direction)i), currentObject);

                changed |= updatedSingle;
            }

            if (changed)
            {
                UpdateWallCaps();
            }
        }

        /**
         * Adjusts the connections value based on the given new tile.
         * Returns whether value changed.
         */
        private bool UpdateSingleConnection(Direction direction, PlacedTileObject placedObject)
        {
            SetPerpendicularBlocked(true);
            bool isConnected = (placedObject && placedObject.HasAdjacencyConnector() && placedObject.GetGenericType() == "wall");

            return adjacents.UpdateDirection(direction, isConnected, true);
        }

        /// <summary>
        /// Walls will try to connect to us. Block or unblock that connects if we are not facing the wall
        /// </summary>
        private void SetPerpendicularBlocked(bool isBlocked)
        {
            // Door can have rotated in the time between
            doorDirection = GetComponent<PlacedTileObject>().GetDirection();
            if (map == null)
                return;
                // map = GetComponentInParent<TileMap>();

            var neighbourObjects = map.GetNeighbourObjects(TileLayer.Turf, 0, transform.position);
            Direction opposite = TileHelper.GetOpposite(doorDirection);

            MultiAdjacencyConnector wallConnector = null;
            if (neighbourObjects[(int)doorDirection] != null)
                wallConnector = neighbourObjects[(int)doorDirection].GetComponent<MultiAdjacencyConnector>();

            if (wallConnector)
                wallConnector.SetBlockedDirection(opposite, isBlocked);

            // Opposite side of door
            wallConnector = null;
            if (neighbourObjects[(int)opposite] != null)
                wallConnector = neighbourObjects[(int)opposite].GetComponent<MultiAdjacencyConnector>();

            if (wallConnector)
                wallConnector.SetBlockedDirection(doorDirection, isBlocked);
        }

        private void CreateWallCaps(bool isPresent, Direction direction)
        {
            int capIndex = TileHelper.GetDirectionIndex(direction);
            if (isPresent && wallCaps[capIndex] == null)
            {
                
                wallCaps[capIndex] = SpawnWallCap(direction);
                wallCaps[capIndex].name = $"WallCap{capIndex}";
            }
            else if (!isPresent && wallCaps[capIndex] != null)
            {
                EditorAndRuntime.Destroy(wallCaps[capIndex]);
                wallCaps[capIndex] = null;
            }
        }

        private void UpdateWallCaps()
        {
            if (wallCapPrefab == null)
                return;

            // Door may have rotated in the editor
            doorDirection = GetComponent<PlacedTileObject>().GetDirection();
            Direction outFacing = TileHelper.GetNextDir(doorDirection);
                
            bool isPresent = adjacents.Adjacent(outFacing) == 1;
            CreateWallCaps(isPresent, outFacing);

            isPresent = adjacents.Adjacent(TileHelper.GetOpposite(outFacing)) == 1;
            CreateWallCaps(isPresent, TileHelper.GetOpposite(outFacing));
        }

        private void ValidateChildren()
        {
            // Note: This only needs to run on the server, which loads from the scene.
            // Anywhere else (including clients, mostly), doesn't really need this.
            for (int i = transform.childCount - 1; i > 0; --i) {
                var child = transform.GetChild(i);
                if (child.name.StartsWith("WallCap")) {
                    bool success = int.TryParse(child.name.Substring(7), out int num);

                    // Remove if no int, int out of bounds, or duplicate
                    if (!success || num > wallCaps.Length || num < 0 || (wallCaps[num] != null && !ReferenceEquals(wallCaps[num], child.gameObject))) {
                        Debug.LogWarning($"Unusual child found whilst searching for wall caps: {child.name}, deleting");
                        EditorAndRuntime.Destroy(child.gameObject);
                        continue;
                    }

                    wallCaps[num] = child.gameObject;
                }
            }
        }

        /**
         * <summary>Spawns a wall cap facing a direction, with appropriate position & settings</summary>
         * <param name="direction">Direction from the centre of the door</param>
         */
        private GameObject SpawnWallCap(Direction direction)
        {
            var wallCap = EditorAndRuntime.InstantiatePrefab(wallCapPrefab, transform);

            Direction cardinalDirectionInput = TileHelper.GetRelativeDirection(direction, doorDirection);
            var cardinal = TileHelper.ToCardinalVector(cardinalDirectionInput);
            var rotation = TileHelper.AngleBetween(direction, doorDirection);


            wallCap.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            wallCap.transform.localPosition = new Vector3(cardinal.Item1 * WALL_CAP_DISTANCE_FROM_CENTRE, 0, cardinal.Item2 * WALL_CAP_DISTANCE_FROM_CENTRE);

            return wallCap;
        }

        // WallCap gameobjects, North, East, South, West. Null if not present.
        private GameObject[] wallCaps = new GameObject[4];
        private AdjacencyBitmap adjacents = new AdjacencyBitmap();
    }
}
