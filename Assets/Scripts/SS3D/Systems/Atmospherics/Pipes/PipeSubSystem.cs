using FishNet.Object;
using QuikGraph;
using QuikGraph.Algorithms;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public class PipeSubSystem : NetworkSubSystem
    {
        private sealed record PipeVertice(short X, short Y, byte Layer);

        public event Action OnSystemSetUp;

        private bool _pipesGraphIsDirty;

        /// <summary>
        /// Graph representing all electric devices and connections on the map. Each electric device is a vertice in this graph,
        /// and each electric connection is an edge.
        /// </summary>
        private UndirectedGraph<PipeVertice, Edge<PipeVertice>> _pipesGraph;

        private Dictionary<int, PipeNet> _netPipes;

        private List<IAtmosDevice> _atmosDevices;

        public bool IsSetUp { get; private set; }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _pipesGraph = new();
            _netPipes = new();
            _atmosDevices = new();
            IsSetUp = true;
            OnSystemSetUp?.Invoke();
            Subsystems.Get<AtmosEnvironmentSubSystem>().OnAtmosTick += OnTick;
        }

        public void RemoveAtmosDevice(IAtmosDevice atmosDevice)
        {
            if (_atmosDevices == null)
            {
                return;
            }

            _atmosDevices.Remove(atmosDevice);
        }

        public List<IAtmosPipe> GetPipes(TileLayer layerType)
        {
            List<IAtmosPipe> pipes = new();

            foreach (PipeNet pipeNet in _netPipes.Values)
            {
                pipes.AddRange(pipeNet.GetPipes(layerType));
            }

            return pipes;
        }

        public void RegisterAtmosDevice(IAtmosDevice atmosDevice)
        {
            _atmosDevices.Add(atmosDevice);
        }

        public void RemovePipe(IAtmosPipe pipe)
        {
            if (_netPipes is null)
            {
                return;
            }

            _netPipes[pipe.PipeNetIndex].RemovePipe(pipe);
            _pipesGraph.RemoveVertex(new((short)pipe.WorldOrigin.x, (short)pipe.WorldOrigin.y, (byte)pipe.TileLayer));
            _pipesGraphIsDirty = true;
        }

        public void RegisterPipe(IAtmosPipe pipe)
        {
            PlacedTileObject tileObject = pipe.PlacedTileObject;
            PipeVertice pipeVertice = new((short)tileObject.WorldOrigin.x, (short)tileObject.WorldOrigin.y, (byte)tileObject.Layer);

            _pipesGraph.AddVertex(pipeVertice);
            List<PlacedTileObject> neighbours = tileObject.Connector?.GetConnectedNeighbours();

            foreach (PlacedTileObject neighbour in neighbours)
            {
                // some stuff can be neighbour in the connectable sense, but should not be connected to the pipenet, e.g. atmos mixers and filters.
                if (!neighbour.TryGetComponent(out IAtmosPipe pipeNeighbour))
                {
                    continue;
                }

                PipeVertice neighbourPipe = new((short)neighbour.WorldOrigin.x, (short)neighbour.WorldOrigin.y, (byte)neighbour.Layer);

                _pipesGraph.AddVertex(neighbourPipe);
                _pipesGraph.AddEdge(new(pipeVertice, neighbourPipe));
            }

            _pipesGraphIsDirty = true;
        }

        public bool TryGetAtmosPipe(Vector3 worldPosition, TileLayer layer, out IAtmosPipe atmosPipe)
        {
            atmosPipe = null;
            SingleTileLocation tileLocation = Subsystems.Get<TileSubSystem>().CurrentMap.GetTileLocation(layer, worldPosition) as SingleTileLocation;

            if (tileLocation == null || !tileLocation.TryGetPlacedObject(out PlacedTileObject placedTileObject))
            {
                return false;
            }

            if (!placedTileObject.TryGetComponent(out atmosPipe))
            {
                return false;
            }

            return true;
        }

        public void AddCoreGasses(Vector3 worldPosition, float4 amount, TileLayer layer)
        {
            if (!TryGetAtmosPipe(worldPosition, layer, out IAtmosPipe pipe))
            {
                return;
            }

            _netPipes[pipe.PipeNetIndex].AddCoreGasses(amount);
        }

        public void RemoveCoreGasses(Vector3 worldPosition, float4 amount, TileLayer layer)
        {
            if (!TryGetAtmosPipe(worldPosition, layer, out IAtmosPipe pipe))
            {
                return;
            }

            _netPipes[pipe.PipeNetIndex].RemoveCoreGasses(amount);
        }

        public void SetValve(IAtmosValve valve)
        {
            PlacedTileObject placed = valve.PlacedTileObject;

            if (!valve.IsOpen)
            {
                _pipesGraph.RemoveVertex(new((short)placed.WorldOrigin.x, (short)placed.WorldOrigin.y, (byte)placed.Layer));
            }
            else
            {
                List<PlacedTileObject> neighbours = placed.Connector?.GetNeighbours();
                PipeVertice pipeVertice = new((short)placed.WorldOrigin.x, (short)placed.WorldOrigin.y, (byte)placed.Layer);
                _pipesGraph.AddVertex(pipeVertice);

                foreach (PlacedTileObject neighbour in neighbours)
                {
                    // todo : should check if pipe
                    PipeVertice neighbourPipe = new((short)neighbour.WorldOrigin.x, (short)neighbour.WorldOrigin.y, (byte)neighbour.Layer);

                    _pipesGraph.AddVertex(neighbourPipe);
                    _pipesGraph.AddEdge(new(pipeVertice, neighbourPipe));
                }
            }

            _pipesGraphIsDirty = true;
        }

        private void OnTick(float dt)
        {
            if (_pipesGraphIsDirty)
            {
                UpdateAllNetPipesTopology();
            }

            foreach (IAtmosDevice atmosDevice in _atmosDevices)
            {
                atmosDevice.StepAtmos(dt);
            }

            foreach (PipeNet pipeNet in _netPipes.Values)
            {
                pipeNet.ApplyGasses();
            }
        }

        [Server]
        private void UpdateAllNetPipesTopology()
        {
            Dictionary<PipeVertice, int> components = new();

            // Compute all components in the graph. One component is basically one pipenet.
            // In graph theory, a component is a connected subgraph that is not part of any larger connected subgraph.
            _pipesGraph.ConnectedComponents(components);
            _netPipes.Clear();

            // group all vertice coordinates by the component index they belong to.
            Dictionary<int, List<PipeVertice>> graphs = components.GroupBy(pair => pair.Value).ToDictionary(group => group.Key, group => group.Select(item => item.Key).ToList());

            foreach (int componentIndex in graphs.Keys)
            {
                List<PipeVertice> component = graphs[componentIndex];
                _netPipes[componentIndex] = new();

                foreach (PipeVertice coord in component)
                {
                    TileSubSystem tileSubSystem = Subsystems.Get<TileSubSystem>();
                    SingleTileLocation location = tileSubSystem.CurrentMap.GetTileLocation((TileLayer)coord.Layer, new(coord.X, 0f, coord.Y)) as SingleTileLocation;

                    if (!location.TryGetPlacedObject(out PlacedTileObject placedObject))
                    {
                        continue;
                    }

                    if (!placedObject.TryGetComponent(out IAtmosPipe pipe))
                    {
                        continue;
                    }

                    _netPipes[componentIndex].AddPipe(pipe);

                    pipe.PipeNetIndex = componentIndex;
                }

                _netPipes[componentIndex].Equalize();
            }

            _pipesGraphIsDirty = false;
        }
    }
}
