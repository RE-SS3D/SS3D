using System;
using Mirror;
using UnityEngine;

//Tiles contain a definition of everything within that is fixed to the tile grid (gridlocked), such as turfs, tables, and pipes.

public class Tile_old : NetworkBehaviour
{
    MeshFilter meshFilter;
    Turf turf;
    GameObject[] contents;
}