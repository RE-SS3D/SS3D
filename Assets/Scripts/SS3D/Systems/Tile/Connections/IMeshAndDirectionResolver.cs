using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using SS3D.Systems.Tile.Connections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Interface for classes that help adjacency connectors to determine a given shape and direction for the mesh,
/// given an adjacency map representing connections.
/// </summary>
public interface IMeshAndDirectionResolver
{
    public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap);
}
