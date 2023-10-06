using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Logging;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Good for stuff that connects on the same layer only, horizontally, of the same specific and generic type.
/// </summary>
public class SimpleAdjacencyConnector : NetworkBehaviour, IAdjacencyConnector
{
    /// <summary>
    /// A type that specifies to which objects to connect to. Must be set if cross connect is used.
    /// </summary>
    [Tooltip("Generic ID that adjacent objects must be to count. If empty, any id is accepted.")]
    [SerializeField]
    private TileObjectGenericType _genericType;

    /// <summary>
    /// Specific ID to differentiate objects when the generic is the same.
    /// </summary>
    [Tooltip("Specific ID to differentiate objects when the generic is the same.")]
    [SerializeField]
    private TileObjectSpecificType _specificType;

    [SerializeField] private SimpleConnector _simpleAdjacency;

    [SyncVar(OnChange = nameof(SyncAdjacencies))]
    private byte _syncedConnections;

    private AdjacencyMap _adjacencyMap;
    private MeshFilter _filter;
    private bool _initialized;
    private PlacedTileObject _placedObject;

    public override void OnStartClient()
    {
        base.OnStartClient();
        Setup();
    }

    private void Setup()
    {
        if (!_initialized)
        {
            _adjacencyMap = new AdjacencyMap();
            _filter = GetComponent<MeshFilter>();

            _placedObject = GetComponent<PlacedTileObject>();
            if (_placedObject == null)
            {
                _genericType = TileObjectGenericType.None;
                _specificType = TileObjectSpecificType.None;
            }
            else
            {
                _genericType = _placedObject.GenericType;
                _specificType = _placedObject.SpecificType;
            }
            _initialized = true;
        }
    }

    private void SyncAdjacencies(byte oldValue, byte newValue, bool asServer)
    {
        if (!asServer)
        {
            Setup();

            _adjacencyMap.DeserializeFromByte(newValue);
            UpdateMeshAndDirection();
        }
    }

    public bool UpdateSingle(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour)
    {
        bool isConnected = false;

        if (neighbourObject != null)
        {
            isConnected = (neighbourObject && neighbourObject.HasAdjacencyConnector);
            isConnected &= neighbourObject.GenericType == _genericType || _genericType == TileObjectGenericType.None;
            isConnected &= neighbourObject.SpecificType == _specificType || _specificType == TileObjectSpecificType.None;

            // Update our neighbour as well
            if (isConnected && updateNeighbour)
                neighbourObject.UpdateSingleAdjacency(_placedObject, TileHelper.GetOpposite(dir));
        }

        bool isUpdated = _adjacencyMap.SetConnection(dir, new AdjacencyData(TileObjectGenericType.None, TileObjectSpecificType.None, isConnected));

        if (isUpdated)
        {
            _syncedConnections = _adjacencyMap.SerializeToByte();
            UpdateMeshAndDirection();
        }

        return isUpdated;
    }

    private void UpdateMeshAndDirection()
    {
        MeshDirectionInfo info = new();
        info = _simpleAdjacency.GetMeshAndDirection(_adjacencyMap);
           
        if (_filter == null)
        {
            Log.Warning(this, "Missing mesh {meshDirectionInfo}", Logs.Generic, info);
        }

        _filter.mesh = info.Mesh;

        Quaternion localRotation = transform.localRotation;
        Vector3 eulerRotation = localRotation.eulerAngles;
        localRotation = Quaternion.Euler(eulerRotation.x, info.Rotation, eulerRotation.z);

        transform.localRotation = localRotation;
    }

    public void UpdateAll(PlacedTileObject[] neighbourObjects)
    {
        Setup();

        bool changed = false;
        for (int i = 0; i < neighbourObjects.Length; i++)
        {
            changed |= UpdateSingle((Direction)i, neighbourObjects[i], true);
        }

        if (changed)
        {
            UpdateMeshAndDirection();
        }
    }
}
