using System;
using Mirror;
using UnityEngine;

// The TileManager handles storing everything 

public class TileManager : NetworkBehaviour
{
    public Vector3Int bounds;

    public Tile[] grid;

    // Direction bitflags used by the TileManager
    public enum DIRECTIONS
    {
        NORTH = 1,
        SOUTH = 2,
        EAST = 4,
        WEST = 8,
        UP = 16,
        DOWN = 32
    }

    // Vector3Int getCoordsFromGrid(int index)
    // {
    // 
    // }

    int getLinearIndex(int x, int y, int z)
    {
        return (bounds.y * bounds.x * z) + (bounds.x * y) + x;
    }

    int getLinearIndex(Vector3Int coords)
    {
        return getLinearIndex(coords.x, coords.y, coords.z);
    }

    int getLinearIndex(Tile tile)
    {
        return 1;
    }

    // Gets a tile in a direction from the source
    // int getStep()
    // {

    // }

    // Gets all tiles in a square range
    // int tilesInRange()
    // {

    // }

    // Gets all tiles in a square range, minus the source
    // int tilesInORange()
    // {

    // }

    void adjustBounds(Vector3Int newBounds)
    {
        Tile[] newGrid;
        for(int i = 0; i < grid.Length; i++)
        {

        }
    }

    void generatePrefabMap()
    {
        for(int i = 0; i < bounds.x; i++)
        {
            for(int j = 0; i < bounds.y; j++)
            {
                Tile newTile = new Tile();
                int index = getLinearIndex(newTile);
                grid[index] = newTile;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if(isServer)
        {
            generatePrefabMap();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}