
using UnityEngine;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using Coimbra;
using FishNet.Object;

namespace SS3D.Systems.Tile.Connections
{
    public class DoorAdjacencyConnector : NetworkBehaviour, IAdjacencyConnector
    {
        public enum DoorType
        {
            Single,
            Double
        };

        /** <summary>Based on peculiarities of the model, the appropriate position of the wall cap</summary> */
        private const float WALL_CAP_DISTANCE_FROM_CENTRE = 0f;

        // As is the standard in the rest of the code, wallCap should face east.
        [SerializeField]
        private GameObject wallCapPrefab = null;

        [SerializeField]
        private DoorType doorType;

        private TileMap map;
        public PlacedTileObject placedTileObject;

        public void CleanAdjacencies()
        {
            if (!map)
            {
                map = GetComponentInParent<TileMap>();
            }

            SetPerpendicularBlocked(false);

            var neighbourObjects = map.GetNeighbourPlacedObjects(TileLayer.Turf, transform.position);
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                neighbourObjects[i]?.UpdateSingleAdjacency(placedTileObject,
                    TileHelper.GetOpposite((Direction)i));
            }
        }

        public bool UpdateSingle(Direction direction, PlacedTileObject placedObject, bool updateNeighbours)
        {

            if (UpdateSingleConnection(direction, placedObject))
                UpdateWallCaps();

            // TODO should check if connection was actually updated.
            return true;
        }

        public void UpdateAll(PlacedTileObject[] neighbourObjects)
        {
            if (!map)
                map = GetComponentInParent<TileMap>();

            neighbourObjects = map.GetNeighbourPlacedObjects(TileLayer.Turf, transform.position);
            PlacedTileObject currentObject = GetComponent<PlacedTileObject>();

            bool changed = false;
            for (int i = 0; i < neighbourObjects.Length; i++)
            {
                bool updatedSingle = false;
                updatedSingle = UpdateSingleConnection((Direction)i, neighbourObjects[i]);
                if (updatedSingle && neighbourObjects[i])
                    neighbourObjects[i].UpdateSingleAdjacency(currentObject, TileHelper.GetOpposite((Direction)i));

                changed |= updatedSingle;
            }

            if (changed)
            {
                UpdateWallCaps();
            }
        }

        /// <summary>
        /// Adjusts the connections value based on the given new tile.
        /// </summary>
        /// <returns> Returns whether value changed.</returns>
        private bool UpdateSingleConnection(Direction direction, PlacedTileObject placedObject)
        {
            bool isConnected = (placedObject && placedObject.HasAdjacencyConnector &&
                placedObject.GenericType == TileObjectGenericType.Wall);

            return adjacencyMap.SetConnection(direction, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));
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
                wallCaps[capIndex].Dispose(true);
                wallCaps[capIndex] = null;
            }
        }

        private void UpdateWallCaps()
        {
            if (wallCapPrefab == null)
                return;

            Direction outFacing = TileHelper.GetNextDir(GetDoorDirection());

            bool isPresent = adjacencyMap.HasConnection(outFacing);
            CreateWallCaps(isPresent, outFacing);

            isPresent = adjacencyMap.HasConnection(TileHelper.GetOpposite(outFacing));
            CreateWallCaps(isPresent, TileHelper.GetOpposite(outFacing));
        }

        /**
            * <summary>Spawns a wall cap facing a direction, with appropriate position & settings</summary>
            * <param name="direction">Direction from the centre of the door</param>
            */
        private GameObject SpawnWallCap(Direction direction)
        {
            var wallCap = Instantiate(wallCapPrefab, transform);
            Direction doorDirection = GetDoorDirection();

            Direction cardinalDirectionInput = TileHelper.GetRelativeDirection(direction, doorDirection);
            var cardinal = TileHelper.ToCardinalVector(cardinalDirectionInput);
            float rotation = TileHelper.AngleBetween(direction, doorDirection);


            wallCap.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            wallCap.transform.localPosition = new Vector3(cardinal.Item1 * WALL_CAP_DISTANCE_FROM_CENTRE, 0, cardinal.Item2 * WALL_CAP_DISTANCE_FROM_CENTRE);
            Spawn(wallCap);
            return wallCap;
        }

        private Direction GetDoorDirection()
        {
            if (placedTileObject == null)
            {
                placedTileObject = GetComponent<PlacedTileObject>();
            }

            return placedTileObject.Direction;
        }

        // WallCap gameobjects, North, East, South, West. Null if not present.
        private GameObject[] wallCaps = new GameObject[4];
        private AdjacencyMap adjacencyMap = new AdjacencyMap();
    }

}
