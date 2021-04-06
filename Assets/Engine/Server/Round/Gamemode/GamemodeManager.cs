using Mirror;
using UnityEngine;

namespace SS3D.Engine.Server.Round
{
    /// <summary>
    /// This should handle the Gamemode of a round
    ///
    /// should be put in the RoundManager object
    /// </summary>
    public class GamemodeManager : NetworkBehaviour
    {
        // the gamemode that will start, this will handle roles and the possible objectives
        // public Gamemode gamemode;
        // public Gamemode[] possibleGamemodes;
        
        // the list os possible objectives that the round will have, we should 
        // probably make this a SO
        public GamemodeObjective[] possibleObjectives;
    }
}