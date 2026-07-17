using Coimbra;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using SS3D.Rendering.URP;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.TileMapCreator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Component that is added to every tile object that is part of the tilemap. Tiles are more restrictive and need to have an origin, fixed grid position and direction to face.
    /// </summary>
    public class PlacedTileObject: NetworkBehaviour, IWorldObjectAsset, ITileOccupant
    {
        /// <summary>
        /// Creates a new PlacedTileObject from a TileObjectSO at a given position and direction. 
        /// Uses NetworkServer.Spawn() if a server is running.
        /// </summary>
        public static PlacedTileObject Create(Vector3 worldPosition, Vector2Int origin, Direction dir, TileObjectSo tileObjectSo, int mapId = 0)
        {
            GameObject tileObjectPrefab = Assets.Get<GameObject>(tileObjectSo.PrefabAsset);
            GameObject placedGameObject = Instantiate(tileObjectPrefab);

#if UNITY_SERVER
            ServerVisualsUtility.DisableRenderingComponents(placedGameObject);
#endif

            StampWorldDecalReceivers(placedGameObject);
            Vector3 placedPosition = tileObjectSo.GetPlacedWorldPosition(worldPosition);
            placedGameObject.transform.SetPositionAndRotation(placedPosition, Quaternion.Euler(0, TileHelper.GetRotationAngle(dir), 0));

            PlacedTileObject placedObject = placedGameObject.GetComponent<PlacedTileObject>();
            if (placedObject == null)
            {
                placedObject = placedGameObject.AddComponent<PlacedTileObject>();
            }

            placedObject.Setup(tileObjectSo, origin, worldPosition, dir, mapId);

            if (InstanceFinder.ServerManager != null)
            {
                if (placedObject.GetComponent<NetworkObject>() == null)
                    Log.Information(SubSystems.Get<TileSubSystem>(), "{placedObject} does not have a Network Component and will not be spawned",
                        Logs.Generic, placedObject.NameString);
                else
                    InstanceFinder.ServerManager.Spawn(placedGameObject);
            }

            return placedObject;
        }

        [SerializeField]
#if UNITY_EDITOR
        [ReadOnly]
        [Header("This field is filled automatically by the AssetData system.")]
#endif
        private ObjectAssetReference _asset;

        [SerializeField]
        private TileObjectSo _tileObjectSo;
        private Vector2Int _origin;
        private Direction _dir;
        private int _mapId;

        [SyncVar(OnChange = nameof(SyncAssetId))]
        private ushort _syncAssetId = TileAssetCatalog.InvalidAssetId;

        [SyncVar(OnChange = nameof(SyncOriginX))]
        private int _syncOriginX;

        [SyncVar(OnChange = nameof(SyncOriginY))]
        private int _syncOriginY;

        [SyncVar(OnChange = nameof(SyncWorldOriginX))]
        private int _syncWorldOriginX;

        [SyncVar(OnChange = nameof(SyncWorldOriginY))]
        private int _syncWorldOriginY;

        [SyncVar(OnChange = nameof(SyncDirectionValue))]
        private Direction _syncDirection;

        [SyncVar(OnChange = nameof(SyncLayerValue))]
        private TileLayer _syncLayer;

        [SyncVar(OnChange = nameof(SyncMapIdValue))]
        private int _syncMapId;

        private IAdjacencyConnector _connector;
        private Vector2Int _worldOrigin;
        private bool _clientRegistered;

        /// <summary>
        /// Returns a list of all grids positions that object occupies.
        /// </summary>
        public List<Vector2Int> GridOffsetList => _tileObjectSo.GetGridOffsetList(_dir);

        public Vector2Int Origin => _origin;

        public Vector2Int WorldOrigin => _worldOrigin;

        public TileObjectGenericType GenericType => _tileObjectSo.genericType;

        public TileObjectSpecificType SpecificType => _tileObjectSo.specificType;

        public TileLayer Layer => _tileObjectSo.layer;

        public Direction Direction => _dir;

        public int MapId => _mapId;

        public string NameString => _tileObjectSo.NameString;

        public TileObjectSo tileObjectSO => _tileObjectSo;

        public bool HasAdjacencyConnector => _connector != null;

        public ObjectAssetReference Asset
        {
            get => _asset;
            set
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Log.Warning(this, "Field {fieldName} is being modified in runtime. This should not happen in normal conditions.", Logs.Generic, nameof(Asset));
                }
                _asset = value;
            }
        }
        public IAdjacencyConnector Connector => _connector;

        public override void OnStartClient()
        {
            base.OnStartClient();
            StampWorldDecalReceivers(gameObject);
            ApplySyncedIdentity();
        }

        /// <summary>
        /// OR <see cref="DecalRenderingLayers.ReceiveWorldDecals"/> onto tile renderers so
        /// floor DecalProjectors can hit tiles without painting characters (Default-only).
        /// </summary>
        private static void StampWorldDecalReceivers(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].renderingLayerMask = DecalRenderingLayers.WithWorldDecals(renderers[i].renderingLayerMask);
            }
        }

        /// <summary>
        /// Set up a new PlacedTileObject. SyncVars are published in <see cref="OnStartServer"/> after spawn.
        /// </summary>
        private void Setup(TileObjectSo tileObjectSo, Vector2Int origin, Vector3 worldPosition, Direction dir, int mapId)
        {
            _tileObjectSo = tileObjectSo;
            _origin = origin;
            _dir = dir;
            _mapId = mapId;
            _connector = GetComponent<IAdjacencyConnector>();
            _worldOrigin = new Vector2Int((int)Math.Round(worldPosition.x), (int)Math.Round(worldPosition.z));
            _asset = tileObjectSo.PrefabAsset;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            PublishIdentityToNetwork();
            NetworkObject.OnObserversActive += HandleObserversActive;
            RefreshHostVisibility();
        }

        public override void OnStopServer()
        {
            NetworkObject.OnObserversActive -= HandleObserversActive;
            base.OnStopServer();
        }

        private void HandleObserversActive(NetworkObject _)
        {
            RefreshHostVisibility();
        }

        private void RefreshHostVisibility()
        {
            if (!IsClient)
                return;

            NetworkConnection localConnection = NetworkManager.ClientManager.Connection;
            if (!localConnection.IsValid)
                return;

            NetworkObject.SetRenderersVisible(NetworkObject.Observers.Contains(localConnection), force: true);
            TileLayerVisibilityService.TryApplyPlacedTileObject(this);
        }

        private void PublishIdentityToNetwork()
        {
            if (_tileObjectSo == null)
                return;

            ushort assetId = SubSystems.Get<TileSubSystem>()?.TryGetAssetId(_tileObjectSo) ?? TileAssetCatalog.InvalidAssetId;
            if (assetId == TileAssetCatalog.InvalidAssetId)
            {
                Log.Warning(this, "Could not resolve compact asset id for {tileName}", Logs.Generic, _tileObjectSo.NameString);
            }

            _syncAssetId = assetId;
            _syncOriginX = _origin.x;
            _syncOriginY = _origin.y;
            _syncWorldOriginX = _worldOrigin.x;
            _syncWorldOriginY = _worldOrigin.y;
            _syncDirection = _dir;
            _syncLayer = _tileObjectSo.layer;
            _syncMapId = _mapId;
        }

        private void SyncAssetId(ushort _, ushort __, bool asServer) => ApplySyncedIdentityIfClient(asServer);

        private void SyncOriginX(int _, int __, bool asServer) => ApplySyncedIdentityIfClient(asServer);

        private void SyncOriginY(int _, int __, bool asServer) => ApplySyncedIdentityIfClient(asServer);

        private void SyncWorldOriginX(int _, int __, bool asServer) => ApplySyncedIdentityIfClient(asServer);

        private void SyncWorldOriginY(int _, int __, bool asServer) => ApplySyncedIdentityIfClient(asServer);

        private void SyncDirectionValue(Direction _, Direction __, bool asServer) => ApplySyncedIdentityIfClient(asServer);

        private void SyncLayerValue(TileLayer _, TileLayer __, bool asServer) => ApplySyncedIdentityIfClient(asServer);

        private void SyncMapIdValue(int _, int __, bool asServer) => ApplySyncedIdentityIfClient(asServer);

        private void ApplySyncedIdentityIfClient(bool asServer)
        {
            if (asServer)
                return;

            ApplySyncedIdentity();
        }

        private void ApplySyncedIdentity()
        {
            if (_syncAssetId == TileAssetCatalog.InvalidAssetId)
                return;

            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            if (tileSystem == null)
                return;

            GenericObjectSo asset = tileSystem.GetAsset(_syncAssetId);
            if (asset is not TileObjectSo tileObjectSo)
                return;

            _tileObjectSo = tileObjectSo;
            _origin = new Vector2Int(_syncOriginX, _syncOriginY);
            _worldOrigin = new Vector2Int(_syncWorldOriginX, _syncWorldOriginY);
            _dir = _syncDirection;
            _mapId = _syncMapId;
            _asset = tileObjectSo.PrefabAsset;
            _connector ??= GetComponent<IAdjacencyConnector>();

            if (TryGetComponent(out DoorAdjacencyConnector doorConnector))
                doorConnector.RefreshWallCapsFromSyncedAdjacencies();

            TileLayerVisibilityService.TryApplyPlacedTileObject(this);
            RegisterWithClientMap();
        }

        /// <summary>
        /// On a remote client, insert this object into the client-side tilemap so systems such as vision
        /// can query occupancy. The server already tracks it, so this is skipped on server/host.
        /// </summary>
        private void RegisterWithClientMap()
        {
            if (IsServer || _clientRegistered)
                return;

            _clientRegistered = true;
            SubSystems.Get<TileSubSystem>()?.NotifyClientPlacedObjectStarted(this);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (IsServer || !_clientRegistered)
                return;

            _clientRegistered = false;
            SubSystems.Get<TileSubSystem>()?.NotifyClientPlacedObjectStopped(this);
        }

        /// <summary>
        /// Destroys itself.
        /// </summary>
        [Server]
        public void DestroySelf()
        {
            if (InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.Despawn(gameObject);
            else
                gameObject.Dispose(true);
        }

        public void UpdateAdjacencies()
        {
            if (HasAdjacencyConnector)
                _connector.UpdateAllConnections();
        }

        public void UpdateSingleAdjacency(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
        {
            if (HasAdjacencyConnector)
                _connector.UpdateSingleConnection(dir, neighbourObject, updateNeighbour);
        }

        public SavedPlacedTileObject Save()
        {
            return new SavedPlacedTileObject
            {
                tileObjectSOName = _tileObjectSo.NameString,
                origin = _origin,
                dir = _dir,
            };
        }

        public void SetDirection(Direction dir)
        {
            _dir = dir;

            if (NetworkObject != null && NetworkObject.IsSpawned && IsServer)
                _syncDirection = dir;
        }

        /// <summary>
        /// Is this in front of the other object ?
        /// </summary>
        public bool IsInFront(PlacedTileObject other)
        {
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(other.Direction);
            return WorldOrigin == other.WorldOrigin + diff;
        }

        /// <summary>
        /// Is this behind the other object ?
        /// </summary>
        public bool IsBehind(PlacedTileObject other)
        {
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(other.Direction);
            return WorldOrigin == other.WorldOrigin - diff;
        }

        /// <summary>
        /// Is this on the right of the other object ?
        /// </summary>
        public bool IsOnRight(PlacedTileObject other)
        {
            Direction dirOnRight = TileHelper.GetNextCardinalDir(other.Direction);
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(dirOnRight);
            return WorldOrigin == other.WorldOrigin + diff;
        }

        /// <summary>
        /// Is this on the left of the other object ?
        /// </summary>
        public bool IsOnLeft(PlacedTileObject other)
        {
            Direction dirOnLeft = TileHelper.GetNextCardinalDir(other.Direction);
            Vector2Int diff = TileHelper.CoordinateDifferenceInFrontFacingDirection(dirOnLeft);
            return WorldOrigin == other.WorldOrigin - diff;
        }

        /// <summary>
        /// Is this at the direction of the other object ? (has to be adjacent).
        /// </summary>
        public bool AtDirectionOf(PlacedTileObject other, Direction dir)
        {
            Tuple<int, int> vector = TileHelper.ToCardinalVector(dir);
            Vector2Int expectedOrigin = other.WorldOrigin + new Vector2Int(vector.Item1, vector.Item2);
            return WorldOrigin == expectedOrigin;
        }

        public bool HasNeighbourFrontBack(List<PlacedTileObject> neighbours,
    out PlacedTileObject inFrontOrBack, bool front)
        {
            foreach (var neighbour in neighbours)
            {
                if (front && neighbour.IsInFront(this))
                {
                    inFrontOrBack = neighbour;
                    return true;
                }

                if (!front && neighbour.IsBehind(this))
                {
                    inFrontOrBack = neighbour;
                    return true;
                }
            }

            inFrontOrBack = null;
            return false;
        }

        public bool HasNeighbourOnSide(List<PlacedTileObject> neighbours,
            out PlacedTileObject onSide, bool left)
        {
            foreach (var neighbour in neighbours)
            {
                if (!left && neighbour.IsOnRight(this))
                {
                    onSide = neighbour;
                    return true;
                }

                if (left && neighbour.IsOnLeft(this))
                {
                    onSide = neighbour;
                    return true;
                }
            }

            onSide = null;
            return false;
        }

        public bool HasNeighbourAtDirection(List<PlacedTileObject> neighbours, out PlacedTileObject atDirection, Direction dir)
        {
            foreach (var neighbour in neighbours)
            {
                if (AtDirectionOf(neighbour, dir))
                {
                    atDirection = neighbour;
                    return true;
                }
            }

            atDirection = null;
            return false;
        }
        /// Other is a neighbour, placed at some direction from this.
        /// </summary>
        /// <param name="other">another placedTileObject, which should be neighbouring this.</param>
        /// <param name="direction"> the found direction, north by default</param>
        /// <returns>true if other is a neighbour of this in term of coordinates</returns>
        public bool NeighbourAtDirectionOf(PlacedTileObject other, out Direction direction)
        {
            direction = Direction.North;
            if (other == null) return false;
            Vector2Int coordinateDifference = other.WorldOrigin - WorldOrigin;

            if(coordinateDifference == Vector2Int.up)
                direction = Direction.North;
            else if(coordinateDifference == Vector2Int.down) 
                direction = Direction.South;
            else if (coordinateDifference == Vector2Int.left)
                direction = Direction.West;
            else if (coordinateDifference == Vector2Int.right)
                direction = Direction.East;
            else if (coordinateDifference == Vector2Int.up + Vector2Int.right)
                direction = Direction.NorthEast;
            else if (coordinateDifference == Vector2Int.up + Vector2Int.left)
                direction = Direction.NorthWest;
            else if (coordinateDifference == Vector2Int.down + Vector2Int.left)
                direction = Direction.SouthWest;
            else if (coordinateDifference == Vector2Int.down + Vector2Int.right)
                direction = Direction.SouthEast;
            else return false;

            return true;
        }
    }
}
