
using FishNet.Object;
using UnityEngine;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;

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

        public Direction DoorDirection => ResolveDoorDirection();

        protected override IMeshAndDirectionResolver AdjacencyResolver => null;

        // Based on peculiarities of the model, the appropriate position of the wall cap
        private const float WALL_CAP_DISTANCE_FROM_CENTRE = 0f;

        // As is the standard in the rest of the code, wallCap should face east.
        [SerializeField]
        private GameObject wallCapPrefab = null;

        [SerializeField]
        private DoorType doorType;

        // WallCap gameobjects, North, East, South, West. Null if not present.
        private GameObject[] wallCaps = new GameObject[4];

        protected override void UpdateMeshAndDirection()
        {
            base.UpdateMeshAndDirection();
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

                wallCaps[capIndex] = CreateWallCap(direction);
                wallCaps[capIndex].name = $"WallCap{capIndex}";
            }
            else if (!isPresent && wallCaps[capIndex] != null)
            {
                Object.DestroyImmediate(wallCaps[capIndex]);
                wallCaps[capIndex] = null;
            }
        }

        private void UpdateWallCaps()
        {
            if (wallCapPrefab == null || _adjacencyMap == null)
                return;

            Direction outFacing = TileHelper.GetNextCardinalDir(DoorDirection);

            bool isPresent = _adjacencyMap.HasConnection(outFacing);
            CreateWallCaps(isPresent, outFacing);

            isPresent = _adjacencyMap.HasConnection(TileHelper.GetOpposite(outFacing));
            CreateWallCaps(isPresent, TileHelper.GetOpposite(outFacing));
        }

        /// <summary> Creates a local wall cap facing a direction, with appropriate position and settings. </summary>
        ///<param name="direction">Direction from the centre of the door</param>
        private GameObject CreateWallCap(Direction direction)
        {
            GameObject wallCap = Instantiate(wallCapPrefab, transform);

            if (wallCap.TryGetComponent(out NetworkObject networkObject))
                Object.DestroyImmediate(networkObject);

            Direction cardinalDirectionInput = TileHelper.GetRelativeDirection(direction, DoorDirection);
            var cardinal = TileHelper.ToCardinalVector(cardinalDirectionInput);
            float rotation = TileHelper.AngleBetween(direction, DoorDirection);

            wallCap.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            wallCap.transform.localPosition = new Vector3(cardinal.Item1 * WALL_CAP_DISTANCE_FROM_CENTRE, 0, cardinal.Item2 * WALL_CAP_DISTANCE_FROM_CENTRE);
            return wallCap;
        }

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            return (neighbourObject && neighbourObject.HasAdjacencyConnector &&
                neighbourObject.GenericType == TileObjectGenericType.Wall);
        }

        /// <summary>
        /// Rebuilds wall caps after replicated tile identity is available on the client.
        /// </summary>
        public void RefreshWallCapsFromSyncedAdjacencies()
        {
            Setup();
            UpdateWallCaps();
        }

        private Direction ResolveDoorDirection()
        {
            if (_placedObject != null)
                return _placedObject.Direction;

            int directionIndex = Mathf.RoundToInt(transform.eulerAngles.y / 45f) % 8;
            if (directionIndex < 0)
                directionIndex += 8;

            return (Direction)directionIndex;
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
