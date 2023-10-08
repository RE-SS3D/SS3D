using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using SS3D.Systems.Tile.Connections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IMeshAndDirectionResolver
{
    public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap);
}
