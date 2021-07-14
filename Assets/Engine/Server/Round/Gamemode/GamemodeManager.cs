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
        public static GamemodeManager singleton;
        // the gamemode that will start, this will handle roles and the possible objectives
        public Gamemode gamemode;
        public Gamemode[] possibleGamemodes;
        
        private void Awake()
        {
            InitializeSingleton();
        }
        
        // called to setup the gamemode at the start of the round
        public void InitiateGamemode()
        {
            Debug.Log("Initializing gamemode");
            if (gamemode == null)
                gamemode = possibleGamemodes[0];
            
            Debug.Log("calling gamemode.setup");
            gamemode.Setup();
        }

        // called to end the current round's gamemode
        public void FinalizeGamemode()
        {
            gamemode.Finalize();
        }
        
        void InitializeSingleton()
        {
            if (singleton != null && singleton != this) { 
                Destroy(gameObject);
            }
            else
            {
                singleton = this;   
            }
        }
    }
}