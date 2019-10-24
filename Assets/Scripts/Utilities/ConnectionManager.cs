using System;
using Mirror;
using UnityEngine;

// Connection managers will handle calculating an object's interactions with neighboring objects that are defined as connectible, such as walls and tables.

public class ConnectionManager : Component
{
    TileManager tileMap;
    GameObject parent;
    Tile[] neighbors;
    int[] selfConnections;
    int[] otherConnections;
    
    enum selfType;
    enum connectionTypes;
    
    void updateConnections()
    {
        if(!neighbors)
        {
            neighbors = tileMap.getTilesInORange(parent);
        }
        
        int[] selfConnectionDirs;
        int[] otherConnectionDirs;
        
        foreach(Tile neighbor in neighbors)
        {
            foreach(GameObject object in neighbor.contents)
            {
                ConnectionManager neighborConnection = object.GetComponent("ConnectionManager") as ConnectionManager;
                if(!neighborConnection)
                    continue
                bool success = false;
                // Compare connections here
                if(success)
                {
                    
                }
            }
        }
        
//         selfConnections = dirsToCornerStates(selfConnectionDirs);
//         otherconnections = dirsToCornerStates(otherConnectionDirs);
    }
    
//     int[] dirsToCornerStates(int[] dirs)
//     {

//     }
}
