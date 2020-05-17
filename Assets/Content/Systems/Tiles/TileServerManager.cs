using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Content.Structures.Fixtures;
using SS3D.Engine.Tiles;
using UnityEngine;

/// <summary>
/// This class is for performing server/client communication.
/// The reason this class exists is because nested objects cannot perform server/client interaction.
///
/// Rules:
/// - NO LOGIC IN HERE. Logic should be in its own components
/// - This component is attached to the tilemap root, therefore is not authoritative.
///   Thus, all communication must be server-to-client.
/// </summary>
[RequireComponent(typeof(TileManager))]
public class TileServerManager : NetworkBehaviour
{
    public void Start()
    {
        tileManager = GetComponent<TileManager>();
    }

    [Server]
    public void SetDoorOpen(GameObject tile, bool open)
    {
        var pos = tileManager.GetIndexAt(tile.transform.position);
        RpcSetDoorOpen(pos.x, pos.y, open ? 1 : 0);
    }

    /// <summary>
    /// Called by a server-side script to open a door
    /// </summary>
    [ClientRpc]
    private void RpcSetDoorOpen(int x, int y, int open)
    {
        if (!isServer) { 
            tileManager.GetTile(x, y).GetComponentInChildren<DoorOpener>().SetOpen(open == 1);
        }
    }

    private TileManager tileManager;
}
