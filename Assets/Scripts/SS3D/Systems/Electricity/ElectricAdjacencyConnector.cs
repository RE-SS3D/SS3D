using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using System.Linq;

namespace System.Electricity
{
    /// <summary>
    /// Base class for stuff that should connect in an electric circuit.
    /// </summary>
    public abstract class ElectricAdjacencyConnector : NetworkActor, IAdjacencyConnector
    {
        /// <summary>
        /// The placed object for this disposal pipe.
        /// </summary>
        protected PlacedTileObject PlacedObject { get; private set; }

        protected bool Initialized { get; private set; }

        public List<PlacedTileObject> GetConnectedNeighbours()
        {
            return GetNeighbours().Where(IsConnected).ToList();
        }

        public List<PlacedTileObject> GetNeighbours()
        {
            Setup();
            List<PlacedTileObject> neighbours = GetElectricDevicesOnSameTile();
            neighbours.AddRange(GetNeighbourElectricDevicesOnSameLayer());
            neighbours.RemoveAll(x => x == null);
            return neighbours;
        }

        public bool IsConnected(PlacedTileObject neighbourObject) => neighbourObject && neighbourObject.Connector is ElectricAdjacencyConnector;

        public abstract void UpdateAllConnections();

        public abstract bool UpdateSingleConnection(Direction dir, PlacedTileObject neighbourObject, bool updateNeighbour);

        protected virtual void Setup()
        {
            if (!Initialized)
            {
                PlacedObject = GetComponent<PlacedTileObject>();
                Initialized = true;
            }
        }

        private List<PlacedTileObject> GetElectricDevicesOnSameTile()
        {
            TileSubSystem tileSubSystem = Subsystems.Get<TileSubSystem>();
            TileMap map = tileSubSystem.CurrentMap;

            List<PlacedTileObject> devicesOnSameTile = new();

            TileChunk currentChunk = map.GetChunk(PlacedObject.gameObject.transform.position);
            List<ITileLocation> deviceLocations = currentChunk.GetTileLocations(PlacedObject.Origin.x, PlacedObject.Origin.y);

            foreach (ITileLocation location in deviceLocations)
            {
                foreach (PlacedTileObject tileObject in location.GetAllPlacedObject())
                {
                    if (tileObject.gameObject.TryGetComponent(out IElectricDevice device))
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
            TileSubSystem tileSubSystem = Subsystems.Get<TileSubSystem>();
            TileMap map = tileSubSystem.CurrentMap;
            IEnumerable<PlacedTileObject> electricNeighbours = map.GetCardinalNeighbourPlacedObjects(
                PlacedObject.Layer,
                PlacedObject.gameObject.transform.position).Where(x => x != null && x.gameObject.TryGetComponent(out IElectricDevice device));

            return electricNeighbours.ToList();
        }
    }
}
