using SS3D.Engine.Tiles;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAtmosLoop
{
    void Initialize();
    void Step();
    void SetTileNeighbour(TileObject tile, int index);
    void SetAtmosNeighbours();
}
