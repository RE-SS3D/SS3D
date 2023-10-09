
using UnityEngine;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using Coimbra;
using FishNet.Object;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for doors, handling adding wall caps, creating custom floor tile under the door.
    /// TODO : add the custom floor.
    /// </summary>
    public class DoorAdjacencyConnector : AbstractHorizontalConnector, IAdjacencyConnector
    {
        private enum DoorType
        {
            Single,
            Double
        };

        public Direction DoorDirection => _placedObject.Direction;

        protected override IMeshAndDirectionResolver AdjacencyResolver => null;

        /** <summary>Based on peculiarities of the model, the appropriate position of the wall cap</summary> */
        private const float WALL_CAP_DISTANCE_FROM_CENTRE = 0f;

        // As is the standard in the rest of the code, wallCap should face east.
        [SerializeField]
        private GameObject wallCapPrefab = null;

        [SerializeField]
        private DoorType doorType;

        // WallCap gameobjects, North, East, South, West. Null if not present.
        private GameObject[] wallCaps = new GameObject[4];

        public override bool UpdateSingleConnection(Direction direction, PlacedTileObject placedObject, bool updateNeighbours)
        {
            bool update = base.UpdateSingleConnection(direction, placedObject, updateNeighbours);
            if (update)
                UpdateWallCaps();
            return update;
        }

        public override void UpdateAllConnections(PlacedTileObject[] neighbourObjects)
        {
            base.UpdateAllConnections(neighbourObjects);
            UpdateWallCaps();
        }

        /// <summary>
        /// Destroy or add a wall cap.
        /// </summary>
        private void CreateWallCaps(bool isPresent, Direction direction)
        {
            int capIndex = GetWallCapIndex(direction);
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

            Direction outFacing = TileHelper.GetNextCardinalDir(DoorDirection);

            bool isPresent = _adjacencyMap.HasConnection(outFacing);
            CreateWallCaps(isPresent, outFacing);

            isPresent = _adjacencyMap.HasConnection(TileHelper.GetOpposite(outFacing));
            CreateWallCaps(isPresent, TileHelper.GetOpposite(outFacing));
        }

        
        /// <summary> Spawns a wall cap facing a direction, with appropriate position & settings </summary>
        ///<param name="direction">Direction from the centre of the door</param>
        private GameObject SpawnWallCap(Direction direction)
        {
            var wallCap = Instantiate(wallCapPrefab, transform);

            Direction cardinalDirectionInput = TileHelper.GetRelativeDirection(direction, DoorDirection);
            var cardinal = TileHelper.ToCardinalVector(cardinalDirectionInput);
            float rotation = TileHelper.AngleBetween(direction, DoorDirection);


            wallCap.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            wallCap.transform.localPosition = new Vector3(cardinal.Item1 * WALL_CAP_DISTANCE_FROM_CENTRE, 0, cardinal.Item2 * WALL_CAP_DISTANCE_FROM_CENTRE);
            Spawn(wallCap);
            return wallCap;
        }

        public override bool IsConnected(Direction dir, PlacedTileObject neighbourObject)
        {
            return (neighbourObject && neighbourObject.HasAdjacencyConnector &&
                neighbourObject.GenericType == TileObjectGenericType.Wall);
        }

        /// <summary>
        /// Get the index of a wallcap in the wallcap Array.
        /// </summary>
        private int GetWallCapIndex(Direction dir)
        {
            return (int)dir / 2;
        }
    }

}
