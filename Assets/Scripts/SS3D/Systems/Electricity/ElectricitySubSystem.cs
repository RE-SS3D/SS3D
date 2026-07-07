using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using QuikGraph;
using QuikGraph.Algorithms;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace System.Electricity
{
    /// <summary>
    /// Handles a graph that contains all electricity circuits.
    /// </summary>
    /// <remarks>
    /// Graph topology is marked dirty on changes and rebuilt on the next tick.
    /// Cable placement uses <see cref="ITileMutationObserver"/> to refresh device edges.
    /// </remarks>
    public class ElectricitySubSystem : NetworkSubSystem, ITileMutationObserver
    {
        public event Action OnSystemSetUp;

        /// <summary>
        /// Called each time the electricity system updates. Subscribers should use this
        /// instead of Unity update loops.
        /// </summary>
        public event Action OnTick;

        public bool IsSetUp { get; private set; }

        private record VerticeCoordinates(short X, short Y, byte Layer, byte Direction);

        private bool _graphIsDirty;
        private List<Circuit> _circuits;
        private UndirectedGraph<VerticeCoordinates, Edge<VerticeCoordinates>> _electricityGraph;

        [SerializeField]
        private float _tickRate = 0.2f;
        private float _timeElapsed;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _electricityGraph = new();
            _circuits = new();
            AddHandle(FixedUpdateEvent.AddListener(HandleFixedUpdate));
            SubSystems.Get<TileSubSystem>().RegisterTileMutationObserver(this);
            IsSetUp = true;
            OnSystemSetUp?.Invoke();
            OnTick += HandleCircuitsUpdate;
        }

        protected override void OnDestroyed()
        {
            SubSystems.Get<TileSubSystem>()?.UnregisterTileMutationObserver(this);
            base.OnDestroyed();
        }

        [Server]
        private void HandleFixedUpdate(ref EventContext context, in FixedUpdateEvent updateEvent)
        {
            _timeElapsed += Time.deltaTime;

            if (_timeElapsed > _tickRate)
            {
                RpcInvokeOnTick();
                _timeElapsed = 0;
            }
        }

        [Server]
        public void HandleCircuitsUpdate()
        {
            if (_graphIsDirty)
            {
                UpdateAllCircuitsTopology();
                _graphIsDirty = false;
            }

            foreach (Circuit circuit in _circuits)
                circuit.UpdateCircuitPower();
        }

        [Server]
        public void AddElectricalElement(IElectricDevice device)
        {
            PlacedTileObject tileObject = device.TileObject;
            VerticeCoordinates deviceCoordinates = ToCoordinates(tileObject);

            if (!_electricityGraph.ContainsVertex(deviceCoordinates))
                _electricityGraph.AddVertex(deviceCoordinates);

            List<PlacedTileObject> neighbours = tileObject.Connector?.GetNeighbours();
            if (neighbours == null)
                return;

            foreach (PlacedTileObject neighbour in neighbours)
            {
                VerticeCoordinates neighbourCoordinates = ToCoordinates(neighbour);
                if (!_electricityGraph.ContainsVertex(neighbourCoordinates))
                    _electricityGraph.AddVertex(neighbourCoordinates);

                _electricityGraph.AddEdge(new(deviceCoordinates, neighbourCoordinates));
            }

            _graphIsDirty = true;
        }

        [Server]
        public void RemoveElectricalElement(IElectricDevice device)
        {
            if (device?.TileObject == null)
                return;

            _electricityGraph.RemoveVertex(ToCoordinates(device.TileObject));
            _graphIsDirty = true;
        }

        [Server]
        public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
        {
            if (occupant is not PlacedTileObject placed)
                return;

            if (placed.Connector is CablesAdjacencyConnector)
                RefreshNeighbouringElectricDevices(placed);
        }

        [Server]
        public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer)
        {
            if (occupant is not PlacedTileObject placed)
                return;

            if (placed.Connector is CablesAdjacencyConnector)
                RefreshNeighbouringElectricDevices(placed);
        }

        public void OnChunkCreated(TileChunkRef chunk)
        {
        }

        public void OnTileStateChanged(TileCoord coord)
        {
        }

        [Server]
        private void RefreshNeighbouringElectricDevices(PlacedTileObject topologyTile)
        {
            foreach (PlacedTileObject neighbour in ElectricNeighbourLookup.GetNeighbours(topologyTile))
            {
                if (neighbour.TryGetComponent(out IElectricDevice device))
                    RefreshElectricalElement(device);
            }
        }

        [Server]
        private void RefreshElectricalElement(IElectricDevice device)
        {
            RemoveElectricalElement(device);
            AddElectricalElement(device);
        }

        [Server]
        private void UpdateAllCircuitsTopology()
        {
            Dictionary<VerticeCoordinates, int> components = new();
            _electricityGraph.ConnectedComponents(components);
            _circuits.Clear();

            Dictionary<int, List<VerticeCoordinates>> graphs = components.GroupBy(pair => pair.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.Key).ToList());

            foreach (List<VerticeCoordinates> component in graphs.Values)
            {
                _circuits.Add(new());
                foreach (VerticeCoordinates coord in component)
                {
                    TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
                    ITileLocation location = tileSystem.CurrentMap.GetTileLocation(
                        (TileLayer)coord.Layer,
                        new(coord.X, 0f, coord.Y));

                    if (!location.TryGetPlacedObject(out PlacedTileObject placedObject, (Direction)coord.Direction))
                        continue;

                    if (!placedObject.TryGetComponent(out IElectricDevice device))
                        continue;

                    _circuits.Last().AddElectricDevice(device);
                }
            }
        }

        private static VerticeCoordinates ToCoordinates(PlacedTileObject tileObject) =>
            new((short)tileObject.WorldOrigin.x, (short)tileObject.WorldOrigin.y,
                (byte)tileObject.Layer, (byte)tileObject.Direction);

        [ObserversRpc]
        private void RpcInvokeOnTick()
        {
            OnTick?.Invoke();
        }
    }
}
