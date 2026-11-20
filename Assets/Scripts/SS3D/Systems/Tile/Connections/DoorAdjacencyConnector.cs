
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Data;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for doors, handling adding wall caps and the floor visual under the door.
    /// The floor is two half tiles (<see cref="AirlockFloorVisual"/>), each copying the floor on its
    /// side of the door. The server finds those floors and syncs their tile asset ids. A side with no
    /// floor to copy leaves its half open, and the door then stops covering the plenum
    /// (<see cref="CoversPlenum"/>, read by <see cref="TileUnderfloorVisibility"/>) so it shows through.
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

        private AirlockFloorVisual _floorVisual;

        /// <summary>Tile asset id of the floor in front of the door (its facing side); invalid when there is none to copy.</summary>
        [SyncVar(OnChange = nameof(SyncSideFloor))]
        private ushort _frontFloorId = TileAssetCatalog.InvalidAssetId;

        /// <summary>Tile asset id of the floor behind the door; invalid when there is none to copy.</summary>
        [SyncVar(OnChange = nameof(SyncSideFloor))]
        private ushort _backFloorId = TileAssetCatalog.InvalidAssetId;

        private ushort _pendingFrontFloorId = TileAssetCatalog.InvalidAssetId;
        private ushort _pendingBackFloorId = TileAssetCatalog.InvalidAssetId;
        private bool _hasPendingSideFloors;

        public override IConnectionRule ConnectionRule => DoorRule;

        /// <summary>
        /// Both halves wear a floor, so the door hides the plenum and what runs under it. False while
        /// either half is open; doors without a floor visual always cover.
        /// </summary>
        public bool CoversPlenum => floorPrefab == null
            || (FrontFloorId != TileAssetCatalog.InvalidAssetId && BackFloorId != TileAssetCatalog.InvalidAssetId);

        // Unspawned on the server, the latest values wait in the pending fields until OnStartServer.
        private ushort FrontFloorId => _hasPendingSideFloors ? _pendingFrontFloorId : _frontFloorId;

        private ushort BackFloorId => _hasPendingSideFloors ? _pendingBackFloorId : _backFloorId;

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_hasPendingSideFloors)
                PublishSideFloors(_pendingFrontFloorId, _pendingBackFloorId);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!IsServer)
                ApplySideFloors();
        }

        public override void SetAdjacencyConnections(byte horizontalConnections)
        {
            base.SetAdjacencyConnections(horizontalConnections);

            TileMap map = SubSystems.GetOrNull<TileSubSystem>()?.CurrentMap;
            if (map != null)
                RefreshSideFloors(map);
        }

        /// <summary>
        /// Server: re-read the floors in front of and behind the door, sync them, and refresh what the
        /// door covers when that changed. <see cref="TileMap"/> calls this when a turf beside a door
        /// is placed or cleared; the adjacency engine calls it when the door itself is placed.
        /// </summary>
        public void RefreshSideFloors(TileMap map)
        {
            if (floorPrefab == null || map == null)
                return;

            Setup();
            Direction front = DoorDirection;
            ushort frontId = FindCopyableFloorId(map, front);
            ushort backId = FindCopyableFloorId(map, TileHelper.GetOpposite(front));
            if (frontId == FrontFloorId && backId == BackFloorId)
                return;

            _pendingFrontFloorId = frontId;
            _pendingBackFloorId = backId;
            _hasPendingSideFloors = true;

            // NetworkObject first — IsServer NREs when _networkObjectCache is null (EditMode).
            bool spawnedServer = NetworkObject != null && NetworkObject.IsSpawned && IsServer;
            if (spawnedServer || !UnityEngine.Application.isPlaying)
                PublishSideFloors(frontId, backId);

            ApplySideFloors();
            TileUnderfloorVisibility.RefreshUnderfloorAt(map, transform.position);
        }
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
            ApplySideFloors();
            UpdateWallCaps();
        }

        private ushort FindCopyableFloorId(TileMap map, Direction side)
        {
            if (!TileHelper.IsCardinalDirection(side))
                return TileAssetCatalog.InvalidAssetId;

            var offset = TileHelper.ToCardinalVector(side);
            Vector3 cell = transform.position + new Vector3(offset.Item1, 0f, offset.Item2);
            if (!map.TryGetTileLocation(TileLayer.Turf, cell, out ITileLocation location)
                || !location.TryGetPlacedObject(out PlacedTileObject turf)
                || turf == null
                || turf.tileObjectSO == null
                || turf.GenericType != TileObjectGenericType.Floor)
                return TileAssetCatalog.InvalidAssetId;

            if (ResolveHalfMaterial(turf.tileObjectSO) == null)
                return TileAssetCatalog.InvalidAssetId;

            return SubSystems.GetOrNull<TileSubSystem>()?.TryGetAssetId(turf.tileObjectSO) ?? TileAssetCatalog.InvalidAssetId;
        }

        private Material ResolveSyncedHalfMaterial(ushort assetId)
        {
            if (assetId == TileAssetCatalog.InvalidAssetId)
                return null;

            return SubSystems.GetOrNull<TileSubSystem>()?.GetAsset(assetId) is TileObjectSo floor
                ? ResolveHalfMaterial(floor)
                : null;
        }

        private Material ResolveHalfMaterial(TileObjectSo floor)
        {
            if (floor.PrefabAsset == null || !floorPrefab.TryGetComponent(out AirlockFloorVisual visualConfig))
                return null;

            GameObject prefab = Assets.Get<GameObject>(floor.PrefabAsset);
            return AirlockFloorSides.GetHalfMaterial(prefab, visualConfig.AtlasMaterials);
        }

        private void PublishSideFloors(ushort frontId, ushort backId)
        {
            _frontFloorId = frontId;
            _backFloorId = backId;
            _hasPendingSideFloors = false;
        }

        private void SyncSideFloor(ushort _, ushort __, bool asServer)
        {
            if (asServer)
                return;

            ApplySideFloors();
            TileMap map = SubSystems.GetOrNull<TileSubSystem>()?.CurrentMap;
            if (map != null)
                TileUnderfloorVisibility.RefreshUnderfloorAt(map, transform.position);
        }

        private void ApplySideFloors()
        {
            if (_floorVisual == null)
                return;

            Direction front = DoorDirection;
            Material frontMaterial = ResolveSyncedHalfMaterial(FrontFloorId);
            Material backMaterial = ResolveSyncedHalfMaterial(BackFloorId);
            bool frontIsSubmesh0 = AirlockFloorSides.SubmeshToward(front) == 0;

            _floorVisual.Apply(
                AirlockFloorSides.UsesNorthSouthMesh(front),
                frontIsSubmesh0 ? frontMaterial : backMaterial,
                frontIsSubmesh0 ? backMaterial : frontMaterial);
            RefreshHostRenderers();
        }

        private void EnsureFloor()
        {
            if (floorPrefab == null || _floor != null)
                return;

            _floor = Instantiate(floorPrefab, transform);
            _floor.name = "DoorFloor";
            _floor.transform.localPosition = Vector3.zero;
            _floor.transform.rotation = Quaternion.identity;

            // Visual only: the plenum below already supports and collides; the door owns interaction.
            // Destroy dependents first: SimpleExaminable [RequireComponent]s Selectable, and Unity refuses
            // to remove a component another one still requires. Reverse order removes later-added first.
            MonoBehaviour[] behaviours = _floor.GetComponentsInChildren<MonoBehaviour>(true);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = behaviours.Length - 1; i >= 0; i--)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour == null || behaviour is FishNet.Object.NetworkObject or AirlockFloorVisual)
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

            _floorVisual = _floor.GetComponent<AirlockFloorVisual>();
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
