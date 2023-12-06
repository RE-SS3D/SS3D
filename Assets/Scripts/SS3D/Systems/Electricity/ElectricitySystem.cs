using SS3D.Core.Behaviours;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuikGraph;
using Coimbra.Services.PlayerLoopEvents;
using Coimbra.Services.Events;
using QuikGraph.Algorithms;
using SS3D.Systems.Tile;
using SS3D.Utils;
using log4net.Util;
using SS3D.Core;
using System.Linq;
using System.Text.RegularExpressions;

namespace System.Electricity
{
    /// <summary>
    /// This system should handle a big graph containing all circuits.
    /// When removing electrical elements or adding new ones, it should not immediately update, instead, it should mark the graph dirty and
    /// trigger an update. Imagine an explosion destroying 247 circuit elements in a single frame. It if update upon any change, it would
    /// have to update the whole graph 247 times. Instead, each time an element is removed, it should tell it to the electricity system, and
    /// each frame, if an element or more has changed, the graph can update.
    /// </summary>
    public class ElectricitySystem : NetworkSystem
    {

        public event Action OnSystemSetUp;

        public bool IsSetUp { get; private set; }

        private struct VerticeCoordinates
        {
            public short x;
            public short y;
            public byte layer;
            public byte direction;

            public VerticeCoordinates(short xcoordinate, short yccordinate, byte zcoordinate, byte directionCoordinate)
            {
                x = xcoordinate;
                y = yccordinate;
                layer = zcoordinate;
                direction = directionCoordinate;
            }
        }

        private bool _graphIsDirty;

        private List<Circuit> _circuits;



        private UndirectedGraph<VerticeCoordinates, Edge<VerticeCoordinates>> _electricityGraph;


        // Start is called before the first frame update
        protected override void OnStart()
        {       
            _electricityGraph = new UndirectedGraph<VerticeCoordinates, Edge<VerticeCoordinates>>();

            _circuits = new List<Circuit>();

            AddHandle(UpdateEvent.AddListener(HandleUpdate));

            IsSetUp = true;

            OnSystemSetUp?.Invoke();
        }

        // Update is called once per frame
        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (_graphIsDirty)
            {
                _graphIsDirty= false;
                UpdateAllCircuitsTopology();
            }

            foreach (Circuit circuit in _circuits)
            {
                circuit.UpdateCircuitPower();
            }
        }

        public void AddElectricalElement(IElectricDevice device)
        {
            VerticeCoordinates deviceCoordinates = new VerticeCoordinates((short)device.TileObject.WorldOrigin.x, (short)device.TileObject.WorldOrigin.y,
                    (byte)device.TileObject.Layer, (byte)device.TileObject.Direction);

            _electricityGraph.AddVertex
            (
                new VerticeCoordinates
                (
                    (short) device.TileObject.WorldOrigin.x,
                    (short) device.TileObject.WorldOrigin.y,
                    (byte) device.TileObject.Layer,
                    (byte) device.TileObject.Direction
                )
            );

            List<PlacedTileObject> neighbours = device.TileObject.Connector.GetNeighbours();

            foreach(PlacedTileObject neighbour in neighbours)
            {
                VerticeCoordinates neighbourCoordinates = new VerticeCoordinates ((short)neighbour.WorldOrigin.x, (short)neighbour.WorldOrigin.y,
                    (byte)neighbour.Layer, (byte)neighbour.Direction);
                
                _electricityGraph.AddVertex(neighbourCoordinates);
                _electricityGraph.AddEdge(new Edge<VerticeCoordinates>(deviceCoordinates, neighbourCoordinates));
            }

            _graphIsDirty = true;

        }

        public void RemoveElectricalElement(IElectricDevice device)
        {
            if (device == null || device.TileObject == null) return;

            _electricityGraph.RemoveVertex
            (
                new VerticeCoordinates
                (
                    (short)device.TileObject.WorldOrigin.x,
                    (short)device.TileObject.WorldOrigin.y,
                    (byte)device.TileObject.Layer,
                    (byte)device.TileObject.Direction
                )
            );

            _graphIsDirty = true;
        }

        private void UpdateAllCircuitsTopology()
        {
            Dictionary<VerticeCoordinates, int> components = new();

            _electricityGraph.ConnectedComponents(components);
            _circuits.Clear();

            var graphs = components.GroupBy(pair => pair.Value)
                .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Key).ToList()
                );



            foreach (List<VerticeCoordinates> component in graphs.Values)
            {
                _circuits.Add(new Circuit());
                foreach (VerticeCoordinates coord in component)
                {
                    TileSystem tileSystem = Subsystems.Get<TileSystem>();
                    ITileLocation location = tileSystem.CurrentMap.GetTileLocation((TileLayer)coord.layer, new Vector3(coord.x, 0f, coord.y));

                    if (!location.TryGetPlacedObject(out PlacedTileObject placedObject, (Direction)coord.direction)) continue;

                    if (!placedObject.TryGetComponent<IElectricDevice>(out var device)) continue;

                    if (device is IPowerConsumer) _circuits.Last().AddConsumer((IPowerConsumer)device);
                    else if (device is IPowerProducer) _circuits.Last().AddProducer((IPowerProducer)device);
                    else if (device is IPowerStorage) _circuits.Last().AddStorage((IPowerStorage)device);
                }
            }
        }
    }
}
