using Mirror;
using UnityEngine;

namespace SS3D.Engine.Server.Gamemode
{
    [CreateAssetMenu(fileName = "Gamemode", menuName = "Gamemode/NuclearOperation", order = 0)]
    public class NuclearOperationGamemode : Gamemode
    {
        public GameObject endRoundStatsPrefab;
        
        public new void Setup()
        {
            // Set up stuff
        }

        public void Finish()
        {
            
        }
    }
}