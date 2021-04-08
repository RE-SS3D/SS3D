using Mirror;
using UnityEngine;

namespace SS3D.Engine.Server.Gamemode
{
    /// <summary>
    /// This should handle the Gamemode of a round
    ///
    /// should be put with the RoundManager object
    /// </summary>
    public class GamemodeManager : NetworkBehaviour
    {
        // the gamemode that will start, this will handle roles and the possible objectives
        public Gamemode gamemode;
        public Gamemode[] possibleGamemodes;
        
        // called to setup the gamemode at the start of the round
        public void InitiateGamemode()
        {
            gamemode.Setup();
        }

        // called to end the current round's gamemode
        public void FinalizeGamemode()
        {
            gamemode.Finalize();
        }
    }
}