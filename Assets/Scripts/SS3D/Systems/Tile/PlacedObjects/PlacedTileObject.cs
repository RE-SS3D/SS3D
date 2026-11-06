using Coimbra;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Rendering.URP;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.MapEditor;
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
    public class PlacedTileObject: NetworkBehaviour, IWorldObjectAsset, ITileOccupant, IReachBlocker, IReachSurface
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
                // A NetworkBehaviour added after Instantiate is not part of the NetworkObject: pure
                // clients never get it, so the tile is missing from their tilemap. Fix the prefab
                // (TilePlacedObjectPrefabSetup.SetupAll); the fallback only keeps offline paths working.
                if (placedGameObject.GetComponent<NetworkObject>() != null)
                {
                    Log.Warning(SubSystems.Get<TileSubSystem>(), "{prefab} lacks PlacedTileObject on the prefab; remote clients will not see it in their tilemap",
                        Logs.Generic, tileObjectPrefab.name);
                }

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

        /// <summary>Distance from a wall tile's centre to its face.</summary>
        private const float WallFaceOffset = 0.5f;

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

        /// <summary>Remaining structural HP. Negative means uninitialized (treated as max on first damage).</summary>
        [SyncVar]
        private float _syncIntegrityRemaining = -1f;

        [SyncVar(OnChange = nameof(SyncIntegrityStage))]
        private StructuralIntegrityStage _syncIntegrityStage = StructuralIntegrityStage.Intact;

        private IAdjacencyConnector _connector;
        private Vector2Int _worldOrigin;
        private bool _clientRegistered;
        private bool _underfloorHidden;

        /// <summary>
        /// Returns a list of all grids positions that object occupies.
        /// </summary>
        public List<Vector2Int> GridOffsetList => _tileObjectSo.GetGridOffsetList(_dir);

        public Vector2Int Origin => _origin;

        public Vector2Int WorldOrigin => _worldOrigin;

        public TileObjectGenericType GenericType => _tileObjectSo.genericType;

        public TileObjectSpecificType SpecificType => _tileObjectSo.specificType;

        public TileLayer Layer => _tileObjectSo.layer;

        /// <summary>
        /// Walls, windows, girders and airlocks stop a hand reaching past them. An open airlock's
        /// panels slide out of the way, so only its solid colliders still in the path block.
        /// </summary>
        public bool BlocksReach =>
            _tileObjectSo != null &&
            _tileObjectSo.genericType is TileObjectGenericType.Wall or TileObjectGenericType.Door;

        /// <summary>
        /// A wall mount shares its wall's tile, so its colliders sit inside the wall's solid and its
        /// points can't be reached as they are. Reach goes to the wall face on the side the mount faces,
        /// which build rules keep clear of walls; a hand on the far side still has the wall in the way.
        /// </summary>
        public Vector3 GetReachPoint(Vector3 point)
        {
            if (_tileObjectSo == null || _tileObjectSo.layer is not (TileLayer.WallMountHigh or TileLayer.WallMountLow))
            {
                return point;
            }

            Vector3 facing = transform.forward;
            return point + (facing * (Vector3.Dot(transform.position - point, facing) + WallFaceOffset));
        }

        public Direction Direction => _dir;

        public int MapId => _mapId;

        /// <summary>
        /// Local integrity when SyncVars cannot be written (no NetworkObject / not spawned).
        /// EditMode tests and map tiles missing a NetworkObject use this path.
        /// </summary>
        private float _localIntegrityRemaining = -1f;
        private StructuralIntegrityStage _localIntegrityStage = StructuralIntegrityStage.Intact;
        private bool _integrityUsesLocal;

        /// <summary>Remaining structural HP. Negative when not yet initialized.</summary>
        public float IntegrityRemaining => _integrityUsesLocal ? _localIntegrityRemaining : _syncIntegrityRemaining;

        public StructuralIntegrityStage IntegrityStage =>
            _integrityUsesLocal ? _localIntegrityStage : _syncIntegrityStage;

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

        /// <summary>
        /// FishNet runs OnStartNetwork on every behaviour before any OnStartClient. Sibling connectors
        /// (pipes, cables, plenum) read <see cref="GenericType"/> in their OnStartClient, and this component
        /// may sit after them on the prefab, so a pure client must resolve its identity here first.
        /// Server/host already set it in <see cref="Setup"/>.
        /// </summary>
        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            if (_tileObjectSo == null)
                ApplySyncedIdentity();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            StampWorldDecalReceivers(gameObject);
            ApplySyncedIdentity();
            NotifyIntegrityPresentation(IntegrityStage);
            // Pure clients never hit OnStartServer; apply cover hide once identity/map are ready.
            if (!IsServer)
                RefreshHostVisibility();
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
            NetworkObject.OnHostVisibilityUpdated += HandleHostVisibilityUpdated;
            RefreshHostVisibility();
            NotifyIntegrityPresentation(IntegrityStage);
        }

        public override void OnStopServer()
        {
            if (NetworkObject != null)
            {
                NetworkObject.OnObserversActive -= HandleObserversActive;
                NetworkObject.OnHostVisibilityUpdated -= HandleHostVisibilityUpdated;
            }

            base.OnStopServer();
        }

        private void HandleObserversActive(NetworkObject _)
        {
            RefreshHostVisibility();
        }

        /// <summary>
        /// FishNet's spawn/observer paths call <see cref="NetworkObject.SetRenderersVisible"/> directly
        /// (bypassing this method) and can re-enable MeshRenderers after underfloor hide. They can also
        /// hide a tile outside the player's AOI straight after <see cref="OnStartServer"/> showed it,
        /// which Map Editor authoring must undo: placed but invisible, until the next full refresh.
        /// </summary>
        private void HandleHostVisibilityUpdated(bool _, bool nextVisible)
        {
            if (!nextVisible)
            {
                if (IsMapEditorAuthoring())
                    RefreshHostVisibility();
                return;
            }

            // Re-apply cover-hide (forceRenderingOff). FishNet may have set enabled=true for AOI.
            if (ShouldHideUnderfloorMeshes())
                ApplyUnderfloorOcclusion();
            else if (_tileObjectSo != null && TileUnderfloorVisibility.IsUnderfloorLayer(Layer))
            {
                TileUnderfloorVisibility.SetCoverRenderingHidden(gameObject, hidden: false);
                _underfloorHidden = false;
            }
        }

        /// <summary>The last applied cover-hide no longer matches this object's cell and surroundings.</summary>
        public bool IsUnderfloorHideOutOfDate => ShouldHideUnderfloorMeshes() != _underfloorHidden;

        /// <summary>
        /// HashGrid AOI drives host MeshRenderer visibility in play; Map Editor authoring
        /// bypasses AOI so the free-fly camera can see the whole station.
        /// </summary>
        public void RefreshHostVisibility()
        {
            // Guard NetworkObject before IsClient — FishNet's IsClient reads _networkObjectCache
            // with no null check (same pitfall as bare IsServer). Hit opening Map Editor 2026-07-28.
            if (NetworkObject == null || !NetworkObject.IsSpawned)
            {
                // Cover hide does not need FishNet — apply presentation even before spawn/observers.
                ApplyUnderfloorPresentationOnly();
                return;
            }

            if (!IsClient || NetworkManager?.ClientManager == null)
            {
                ApplyUnderfloorPresentationOnly();
                return;
            }

            NetworkConnection localConnection = NetworkManager.ClientManager.Connection;
            if (!localConnection.IsValid)
            {
                ApplyUnderfloorPresentationOnly();
                return;
            }

            bool mapEditor = IsMapEditorAuthoring();
            bool inAoi = mapEditor;
            if (!inAoi)
            {
                // Client-host: FishNet Observers gate MeshRenderers. Pure client: spawn itself is AOI.
                inAoi = !IsServer || NetworkObject.Observers.Contains(localConnection);
            }

            bool shouldHide = ShouldHideUnderfloorMeshes();
            _underfloorHidden = shouldHide;

            if (shouldHide)
            {
                // Host: UnderfloorCoverCondition fails → FishNet removes host from Observers and
                // SetRenderersVisible(false). Remotes stay in Observers for occupancy — hide draw
                // with forceRenderingOff only (never fight FishNet by force-showing).
                ApplyUnderfloorOcclusion();
                if (!inAoi)
                    NetworkObject.SetRenderersVisible(false, force: true);
            }
            else if (inAoi)
            {
                // FishNet SetRenderersVisible only toggles renderers that were enabled when its
                // cache was first built. After AOI disables them, UpdateRenderers can shrink the
                // cache to empty — walls often recover via adjacency churn; floors/plenums stay off.
                // Re-enable children and rebuild the cache before asking FishNet to show them.
                EnableAllChildRenderers();
                SetSelectableEnabled(true);
                NetworkObject.UpdateRenderers(false);
                NetworkObject.SetRenderersVisible(true, force: true);
            }
            else
            {
                NetworkObject.SetRenderersVisible(false, force: true);
            }

            TileLayerVisibilityService.TryApplyPlacedTileObject(this);
            if (shouldHide)
                ApplyUnderfloorOcclusion();
        }

        /// <summary>
        /// MeshRenderer/Selectable only — safe before NetworkObject is spawned or client is ready.
        /// </summary>
        private void ApplyUnderfloorPresentationOnly()
        {
            if (IsMapEditorAuthoring())
            {
                EnableAllChildRenderers();
                SetSelectableEnabled(true);
                _underfloorHidden = false;
                return;
            }

            ApplyUnderfloorOcclusion();
        }

        private void ApplyUnderfloorOcclusion()
        {
            if (!ShouldHideUnderfloorMeshes())
                return;

            // Do not set Renderer.enabled — FishNet AOI owns that. Cover-hide uses forceRenderingOff.
            TileUnderfloorVisibility.SetCoverRenderingHidden(gameObject, hidden: true);
            SetSelectableEnabled(false);
            _underfloorHidden = true;
        }

        private bool ShouldHideUnderfloorMeshes()
        {
            if (_tileObjectSo == null || !TileUnderfloorVisibility.IsUnderfloorLayer(Layer))
                return false;

            if (IsMapEditorAuthoring())
                return false;

            if (!SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null)
                return false;

            // Prefer live transform; WorldOrigin can lag SyncVar apply on pure clients.
            Vector3 world = transform.position;
            world.y = 0f;
            if (TileUnderfloorVisibility.ShouldHideUnderfloor(tiles.CurrentMap, world, Layer, mapEditorAuthoring: false))
                return true;

            Vector3 synced = new Vector3(WorldOrigin.x, 0f, WorldOrigin.y);
            return TileUnderfloorVisibility.ShouldHideUnderfloor(tiles.CurrentMap, synced, Layer, mapEditorAuthoring: false);
        }

        private void SetSelectableEnabled(bool enabled)
        {
            if (TryGetComponent(out SS3D.Systems.Selection.Selectable selectable) && selectable != null)
                selectable.enabled = enabled;
        }

        private static void EnableAllChildRenderers(GameObject root)
        {
            if (root == null)
                return;

            // Clear cover-hide and ensure FishNet can show meshes again when uncovered / Map Editor.
            TileUnderfloorVisibility.SetCoverRenderingHidden(root, hidden: false);
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && !SS3D.Systems.Interactions.InteractionOutlineView.IsOutlineRenderer(renderers[i]))
                    renderers[i].enabled = true;
            }
        }

        private void EnableAllChildRenderers() => EnableAllChildRenderers(gameObject);

        private static bool IsMapEditorAuthoring() =>
            SubSystems.TryGet(out MapEditorSubSystem editor) && editor.IsActive;

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
            // NetworkObject first — IsServer NREs when _networkObjectCache is null (EditMode).
            // Offline/unspawned: skip; server/host already tracks the tile.
            if (_clientRegistered
                || NetworkObject == null
                || !NetworkObject.IsSpawned
                || IsServer)
                return;

            _clientRegistered = true;
            SubSystems.Get<TileSubSystem>()?.NotifyClientPlacedObjectStarted(this);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (!_clientRegistered
                || NetworkObject == null
                || !NetworkObject.IsSpawned
                || IsServer)
                return;

            _clientRegistered = false;
            SubSystems.Get<TileSubSystem>()?.NotifyClientPlacedObjectStopped(this);
        }

        /// <summary>
        /// Server (or EditMode) integrity write for structural turf.
        /// </summary>
        public void ServerSetIntegrity(float remaining, StructuralIntegrityStage stage)
        {
            // FishNet SyncVar setters call SyncBase.IsNetworkInitialized, which touches
            // NetworkBehaviour.IsServer → _networkObjectCache with no null check. Guard like SetDirection.
            if (!CanWriteIntegritySyncVars())
            {
                _integrityUsesLocal = true;
                _localIntegrityRemaining = remaining;
                _localIntegrityStage = stage;
                NotifyIntegrityPresentation(stage);
                return;
            }

            _integrityUsesLocal = false;
            _syncIntegrityRemaining = remaining;
            _syncIntegrityStage = stage;
            // Host/server: SyncVar OnChange may not fire on assign — apply presentation explicitly.
            NotifyIntegrityPresentation(stage);
        }

        private void SyncIntegrityStage(StructuralIntegrityStage _, StructuralIntegrityStage next, bool asServer)
        {
            if (asServer)
                return;

            NotifyIntegrityPresentation(next);
        }

        private void NotifyIntegrityPresentation(StructuralIntegrityStage stage)
        {
            if (!TryGetComponent(out StructuralIntegrityPresenter presenter)
                && SteelWallDamageVfx.SupportsOverlay(this))
                presenter = gameObject.AddComponent<StructuralIntegrityPresenter>();
            if (presenter != null)
                presenter.Apply(stage);
        }

        private bool CanWriteIntegritySyncVars()
        {
            // Woven setter writes the field and skips SyncVar.SetValue when not playing (EditMode tests).
            if (!UnityEngine.Application.isPlaying)
                return true;

            // NetworkObject here is NetworkBehaviour's cached ref — null when PlacedTileObject was
            // AddComponent'd at runtime (doors/floors) and never SerializeComponents'd. Writing SyncVars
            // then NREs inside FishNet SyncBase.IsNetworkInitialized.
            return NetworkObject != null && NetworkObject.IsSpawned && IsServer;
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
