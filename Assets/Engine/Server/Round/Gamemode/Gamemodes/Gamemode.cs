using Mirror;
using SS3D.Engine.Server.Round;
using UnityEngine;

namespace SS3D.Engine.Server.Gamemode
{
    public abstract class Gamemode : ScriptableObject, IGamemode
    {
        // the list os possible objectives that the round will have, we should 
        // probably make this a SO
        public GamemodeObjective[] possibleObjectives;

        // objectives that were assigned to the players
        public GamemodeObjective[] assignedObjectives;
        
        public virtual void Setup()
        {
            // setup gamemode stuff
        }
        
        public virtual void Finalize()
        {
            // validate all objectives            
        }
    }
}