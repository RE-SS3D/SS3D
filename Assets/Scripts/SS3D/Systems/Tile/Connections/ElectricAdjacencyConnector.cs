using SS3D.Core;
using SS3D.Core.Behaviours;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Base class for stuff that should connect in an electric circuit. 
    /// </summary>
    public abstract class ElectricAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {
        /// <summary>
        /// The placed object for this disposal pipe.
        /// </summary>
        protected PlacedTileObject PlacedObject;
        protected bool Initialized;

        protected virtual void Setup()
        {
            if (!Initialized)
            {
                PlacedObject = GetComponent<PlacedTileObject>();
                Initialized = true;
            }
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            Setup();
            List<PlacedTileObject> neighbours = GetElectricDevicesOnSameTile();
            neighbours.AddRange(GetNeighbourElectricDevicesOnSameLayer());
            neighbours.RemoveAll(x => x == null);
            return neighbours;
        }

        public bool IsConnected(PlacedTileObject neighbourObject)
        {
            return neighbourObject?.Connector is ElectricAdjacencyConnector;
        }

        public abstract void UpdateAllConnections();

        public abstract bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour);

        private List<PlacedTileObject> GetElectricDevicesOnSameTile()
        {
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            TileMap map = tileSystem.CurrentMap;

            List<PlacedTileObject> devicesOnSameTile = new();

            TileChunk currentChunk = map.GetChunk(PlacedObject.gameObject.transform.position);
            List<ITileLocation> deviceLocations = currentChunk.GetTileLocations(PlacedObject.Origin.x, PlacedObject.Origin.y);

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

            devicesOnSameTile.Remove(PlacedObject);

            return devicesOnSameTile;
        }

        private List<PlacedTileObject> GetNeighbourElectricDevicesOnSameLayer()
        {
            TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
            TileMap map = tileSystem.CurrentMap;
            IEnumerable<PlacedTileObject> electricNeighbours = map.GetCardinalNeighbourPlacedObjects(PlacedObject.Layer,
                PlacedObject.gameObject.transform.position).Where(x => x!= null && x.gameObject.TryGetComponent(out IElectricDevice device));

            return electricNeighbours.ToList();
        }
    }
}
