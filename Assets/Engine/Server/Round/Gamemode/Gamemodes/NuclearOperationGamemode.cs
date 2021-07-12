using Mirror;
using SS3D.Content;
using SS3D.Engine.Server.Round;
using UnityEngine;

namespace SS3D.Engine.Server.Gamemode
{
    [CreateAssetMenu(fileName = "Gamemode", menuName = "Gamemode/Nuke/Nuclear Operation Gamemode", order = 0)]
    public class NuclearOperationGamemode : Gamemode
    {
        public GameObject endRoundStatsPrefab;
        
        public new void Setup()
        {
            foreach (Entity player in RoundManager.singleton.roundPlayers)
            {
                NukeActivationGamemodeObjective objective = new NukeActivationGamemodeObjective();
                objective.owner = player;
                objective.completed = false;
                
                Debug.Log("Nuke objective added to: " + objective.owner);
            }
        }

        public void Finish()
        {
            
        }
    }
}