using UnityEngine;
using System.Collections;

namespace SS3D.Engine.Tiles.State
{
    /**
     * An interface used as a component for recieving and sending tile state.
     * 
     * Note: Please do not directly inherit from this. Use TileStateMaintainer instead if possible.
     */
    public interface TileStateCommunicator
    {
        object GetTileState();
        void SetTileState(object obj);
    }
}
