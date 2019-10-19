using System;
using Mirror;
using UnityEngine;

public class TileManager : NetworkBehaviour
{
    public Vector3Int bounds;

    public Tile[] grid;

    // Direction bitflags used by the TileManager
    [HideInInspector]
    public const int NORTH = 1;

    [HideInInspector]
    public const int SOUTH = 2;

    [HideInInspector]
    public const int EAST = 4;

    [HideInInspector]
    public const int WEST = 8;

    [HideInInspector]
    public const int UP = 16;

    [HideInInspector]
    public const int DOWN = 32;

    [HideInInspector]
    public int[] CARDINALS = {
        NORTH,
        EAST,
        SOUTH,
        WEST
    };

    [HideInInspector]
    public int[] ORDINALS = {
        NORTH|EAST,
        SOUTH|EAST,
        SOUTH|WEST,
        NORTH|WEST
    };

    // Vector3Int getCoordsFromGrid(int index)
    // {
    //     //return object coords in the index]
    //     Vector3Int coord(1,1,1);
    //     return ;
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
        // return getLinearIndex(tile.x, tile.y, tile.z);
        return 1;
    }

    // void adjustBounds(Vector3Int newBounds)
    // {
    //     Tile[] newGrid;
    //     for(int i = 0; i < grid.Length; i++)
    //     {
    //         // Vector3Int coords = getCoordsFromGrid(i);
    //         // int newIndex = getLinearIndex()
    //     }
    // }

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