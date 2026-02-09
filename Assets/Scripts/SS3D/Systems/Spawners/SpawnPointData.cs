using SS3D.Systems.Roles;
using UnityEngine;

namespace SS3D.Systems.Spawners
{
    [CreateAssetMenu(menuName = "SS3D/Spawners/SpawnPoint", fileName = "SpawnPointData")]
    public class SpawnPointData : ScriptableObject
    {
        [Tooltip("Which roles are allowed to spawn on this point")]
        public RoleData RoleData;
        
        [Tooltip("Defines which type of players will spawn based on conditions like late-joining")]
        public SpawnType SpawnType;
    }
    
    public enum SpawnType
    {
        LateJoin,
        Job,
    }
}