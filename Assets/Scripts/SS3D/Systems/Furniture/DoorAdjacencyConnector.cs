using Coimbra;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for doors, handling adding wall caps, creating custom floor tile under the door.
    /// TODO : add the custom floor.
    /// </summary>
    public class DoorAdjacencyConnector : AbstractHorizontalConnector
    {
        private enum DoorType
        {
            Single = 0,
            Double = 1,
        }

        // Based on peculiarities of the model, the appropriate position of the wall cap
        private const float WallCapDistanceFromCentre = 0f;

        // As is the standard in the rest of the code, wallCap should face east.
        [FormerlySerializedAs("wallCapPrefab")]
        [SerializeField]
        private GameObject _wallCapPrefab;

        [FormerlySerializedAs("doorType")]
        [SerializeField]
        private DoorType _doorType;

        // WallCap gameobjects, North, East, South, West. Null if not present.
        private GameObject[] _wallCaps = new GameObject[4];

        public Direction DoorDirection => PlacedObject.Direction;

        protected override IMeshAndDirectionResolver AdjacencyResolver => null;

        public override bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            bool update = base.UpdateSingleConnection(dir, neighbourObject, updateNeighbour);

            if (update)
            {
                UpdateWallCaps();
            }

            return update;
        }

        public override void UpdateAllConnections()
        {
            base.UpdateAllConnections();
            UpdateWallCaps();
        }

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            return neighbourObject && neighbourObject.HasAdjacencyConnector && neighbourObject.GenericType == TileObjectGenericType.Wall;
        }

        /// <summary>
        /// Destroy or add a wall cap.
        /// </summary>
        private void CreateWallCaps(bool isPresent, Direction direction)
        {
            int capIndex = GetWallCapIndex(direction);
            if (isPresent && !_wallCaps[capIndex])
            {
                _wallCaps[capIndex] = SpawnWallCap(direction);
                _wallCaps[capIndex].name = $"WallCap{capIndex}";
            }
            else if (!isPresent && _wallCaps[capIndex])
            {
                _wallCaps[capIndex].Dispose(true);
                _wallCaps[capIndex] = null;
            }
        }

        private void UpdateWallCaps()
        {
            if (!_wallCapPrefab)
            {
                return;
            }

            Direction outFacing = TileHelper.GetNextCardinalDir(DoorDirection);

            bool isPresent = AdjacencyMap.HasConnection(outFacing);
            CreateWallCaps(isPresent, outFacing);

            isPresent = AdjacencyMap.HasConnection(TileHelper.GetOpposite(outFacing));
            CreateWallCaps(isPresent, TileHelper.GetOpposite(outFacing));
        }

        /// <summary> Spawns a wall cap facing a direction, with appropriate position & settings </summary>
        ///<param name="direction">Direction from the centre of the door</param>
        private GameObject SpawnWallCap(Direction direction)
        {
            GameObject wallCap = Instantiate(_wallCapPrefab, transform);

            Direction cardinalDirectionInput = TileHelper.GetRelativeDirection(direction, DoorDirection);
            Tuple<int, int> cardinal = TileHelper.ToCardinalVector(cardinalDirectionInput);
            float rotation = TileHelper.AngleBetween(direction, DoorDirection);

            wallCap.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            wallCap.transform.localPosition = new Vector3(cardinal.Item1 * WallCapDistanceFromCentre, 0, cardinal.Item2 * WallCapDistanceFromCentre);
            Spawn(wallCap);
            return wallCap;
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
