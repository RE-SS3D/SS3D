using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Furniture;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class ElectricAdjacencyConnector : NetworkActor, IAdjacencyConnector
{
    /// <summary>
    /// The placed object for this disposal pipe.
    /// </summary>
    protected PlacedTileObject _placedObject;

    private bool _initialized;

    protected void Setup()
    {
        if (!_initialized)
        {
            _placedObject = GetComponent<PlacedTileObject>();
        }
    }

    public List<PlacedTileObject> GetNeighbours()
    {
        List<PlacedTileObject> neighbours = GetElectricDevicesOnSameTile();
        neighbours.AddRange(GetNeighbourElectricDevicesOnSameLayer());
        neighbours.RemoveAll(x => x == null);
        return neighbours;
    }

    public bool IsConnected(PlacedTileObject neighbourObject)
    {
        return neighbourObject.Connector is ElectricAdjacencyConnector;
    }

    public abstract void UpdateAllConnections();

    public abstract bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour);

    private List<PlacedTileObject> GetElectricDevicesOnSameTile()
    {
        TileSystem tileSystem = Subsystems.Get<TileSystem>();
        var map = tileSystem.CurrentMap;

        List<PlacedTileObject> devicesOnSameTile = new();

        TileChunk currentChunk = map.GetChunk(_placedObject.gameObject.transform.position);
        List<ITileLocation> deviceLocations = currentChunk.GetTileLocations(_placedObject.Origin.x, _placedObject.Origin.y);

        foreach(ITileLocation location in deviceLocations)
        {
            foreach(PlacedTileObject tileObject in location.GetAllPlacedObject())
            {
                if(tileObject.gameObject.TryGetComponent(out IElectricDevice device))
                {
                    devicesOnSameTile.Add(tileObject);
                }
            }
        }

        return devicesOnSameTile;
    }

    private List<PlacedTileObject> GetNeighbourElectricDevicesOnSameLayer()
    {
        List<PlacedTileObject> neighbours = new();
        TileSystem tileSystem = Subsystems.Get<TileSystem>();
        var map = tileSystem.CurrentMap;
        neighbours.AddRange(map.GetNeighbourPlacedObjects(_placedObject.Layer,
            _placedObject.gameObject.transform.position).Where(x => x.gameObject.TryGetComponent(out IElectricDevice device)));
        return neighbours;
    }
}
