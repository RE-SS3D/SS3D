
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for doors, handling adding wall caps and the floor visual under the door.
    /// Doors occupy the Turf layer, so they cover the plenum (see <see cref="TileUnderfloorVisibility"/>);
    /// without their own floor the covered cell would render as open space.
    /// </summary>
    public class DoorAdjacencyConnector : EngineDrivenHorizontalConnector
    {
        private enum DoorType
        {
            Single,
            Double
        };

        private static readonly DoorConnectionRule DoorRule = new();

        public Direction DoorDirection => ResolveDoorDirection();

        protected override IMeshAndDirectionResolver AdjacencyResolver => null;

        protected override IMeshAndDirectionResolver EngineMeshResolver => null;

        private const float WALL_CAP_DISTANCE_FROM_CENTRE = 0f;

        [SerializeField]
        private GameObject wallCapPrefab = null;

        [SerializeField]
        private DoorType doorType;

        [SerializeField]
        [Tooltip("Visual-only floor instantiated under the door. Network, collider and interaction components are stripped.")]
        private GameObject floorPrefab = null;

        private GameObject[] wallCaps = new GameObject[4];

        private GameObject _floor;

        public override IConnectionRule ConnectionRule => DoorRule;

        /// <summary>
        /// Rebuilds wall caps after replicated tile identity is available on the client.
        /// </summary>
        public void RefreshWallCapsFromSyncedAdjacencies()
        {
            Setup();
            ApplyEngineConnections(SyncedEngineConnections);
        }

        protected override void ApplyEngineConnections(byte horizontalConnections)
        {
            Setup();
            if (_adjacencyMap == null)
                return;

            _adjacencyMap.DeserializeFromByte(horizontalConnections);
            EnsureFloor();
            UpdateWallCaps();
        }

        private void EnsureFloor()
        {
            if (floorPrefab == null || _floor != null)
                return;

            _floor = Instantiate(floorPrefab, transform);
            _floor.name = "DoorFloor";
            _floor.transform.localPosition = Vector3.zero;
            _floor.transform.localRotation = Quaternion.identity;

            // Visual only: the plenum below already supports and collides; the door owns interaction.
            // Destroy dependents first: SimpleExaminable [RequireComponent]s Selectable, and Unity refuses
            // to remove a component another one still requires. Reverse order removes later-added first.
            MonoBehaviour[] behaviours = _floor.GetComponentsInChildren<MonoBehaviour>(true);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = behaviours.Length - 1; i >= 0; i--)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour == null || behaviour is FishNet.Object.NetworkObject)
                        continue;

                    if (pass == 0 && behaviour is SS3D.Systems.Selection.Selectable)
                        continue;

                    Object.DestroyImmediate(behaviour);
                }
            }

            foreach (Collider floorCollider in _floor.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(floorCollider);

            if (_floor.TryGetComponent(out FishNet.Object.NetworkObject networkObject))
                Object.DestroyImmediate(networkObject);

#if UNITY_SERVER
            ServerVisualsUtility.DisableRenderingComponents(_floor);
#endif

            RefreshHostRenderers();
        }

        private void RefreshHostRenderers()
        {
            // Children are Instantiated after FishNet may have cached renderers; refresh host AOI visibility
            // so newly added MeshRenderers hide with the parent door when out of observers.
            if (TryGetComponent(out PlacedTileObject placed))
                placed.RefreshHostVisibility();
            else if (TryGetComponent(out FishNet.Object.NetworkObject nob) && nob.IsSpawned)
                nob.UpdateRenderers(false);
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

            if (wallCap.TryGetComponent(out FishNet.Object.NetworkObject networkObject))
                Object.DestroyImmediate(networkObject);

#if UNITY_SERVER
            ServerVisualsUtility.DisableRenderingComponents(wallCap);
#endif

            Direction cardinalDirectionInput = TileHelper.GetRelativeDirection(direction, DoorDirection);
            var cardinal = TileHelper.ToCardinalVector(cardinalDirectionInput);
            float rotation = TileHelper.AngleBetween(direction, DoorDirection);

            wallCap.transform.localRotation = Quaternion.Euler(0, rotation, 0);
            wallCap.transform.localPosition = new Vector3(cardinal.Item1 * WALL_CAP_DISTANCE_FROM_CENTRE, 0, cardinal.Item2 * WALL_CAP_DISTANCE_FROM_CENTRE);

            RefreshHostRenderers();

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
