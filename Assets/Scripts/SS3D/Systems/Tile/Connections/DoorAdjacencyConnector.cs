
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for doors, handling adding wall caps, creating custom floor tile under the door.
    /// TODO : add the custom floor.
    /// </summary>
    public class DoorAdjacencyConnector : AbstractHorizontalConnector, IAdjacencyConnector, IEngineDrivenAdjacency
    {
        private enum DoorType
        {
            Single,
            Double
        };

        private static readonly DoorConnectionRule DoorRule = new();

        public Direction DoorDirection => ResolveDoorDirection();

        protected override IMeshAndDirectionResolver AdjacencyResolver => null;

        private const float WALL_CAP_DISTANCE_FROM_CENTRE = 0f;

        [SerializeField]
        private GameObject wallCapPrefab = null;

        [SerializeField]
        private DoorType doorType;

        private GameObject[] wallCaps = new GameObject[4];

        [SyncVar(OnChange = nameof(SyncEngineConnections))]
        private byte _syncedEngineConnections;

        private byte _pendingEngineConnections;
        private bool _hasPendingEngineConnections;

        public IConnectionRule ConnectionRule => DoorRule;

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_hasPendingEngineConnections)
                PublishEngineConnections(_pendingEngineConnections);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyEngineSyncedAdjacencies();
        }

        public void SetAdjacencyConnections(byte horizontalConnections)
        {
            _pendingEngineConnections = horizontalConnections;
            _hasPendingEngineConnections = true;
            ApplyConnectionsToWallCaps(horizontalConnections);

            if (NetworkObject != null && NetworkObject.IsSpawned)
                PublishEngineConnections(horizontalConnections);
        }

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            return ConnectionRule.IsConnected(GetComponentInParent<PlacedTileObject>(), neighbourObject);
        }

        public override void UpdateAllConnections()
        {
            TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
            if (map == null)
                return;

            map.AdjacencyEngine.QueueCascadeFrom(PlacedObject);
            map.AdjacencyEngine.ProcessQueue();
        }

        public override bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
            if (map == null)
                return false;

            map.AdjacencyEngine.QueueUpdate(PlacedObject);

            if (updateNeighbour && neighbourObject != null && neighbourObject.TryGetComponent<IEngineDrivenAdjacency>(out _))
                map.AdjacencyEngine.QueueUpdate(neighbourObject);

            map.AdjacencyEngine.ProcessQueue();
            return true;
        }

        protected override void UpdateMeshAndDirection()
        {
        }

        /// <summary>
        /// Rebuilds wall caps after replicated tile identity is available on the client.
        /// </summary>
        public void RefreshWallCapsFromSyncedAdjacencies()
        {
            Setup();
            ApplyConnectionsToWallCaps(_syncedEngineConnections);
        }

        private void ApplyConnectionsToWallCaps(byte horizontalConnections)
        {
            Setup();
            if (_adjacencyMap == null)
                return;

            _adjacencyMap.DeserializeFromByte(horizontalConnections);
            UpdateWallCaps();
        }

        private void ApplyEngineSyncedAdjacencies()
        {
            ApplyConnectionsToWallCaps(_syncedEngineConnections);
        }

        private void PublishEngineConnections(byte connections)
        {
            _syncedEngineConnections = connections;
            _hasPendingEngineConnections = false;
        }

        private void SyncEngineConnections(byte _, byte newValue, bool asServer)
        {
            if (!asServer)
                ApplyConnectionsToWallCaps(newValue);
        }

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

        private Direction ResolveDoorDirection()
        {
            if (_placedObject != null)
                return _placedObject.Direction;

            int directionIndex = Mathf.RoundToInt(transform.eulerAngles.y / 45f) % 8;
            if (directionIndex < 0)
                directionIndex += 8;

            return (Direction)directionIndex;
        }

        private int GetWallCapIndex(Direction dir)
        {
            return (int)dir / 2;
        }
    }
}
