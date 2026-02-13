using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Tile;

namespace SS3D.Systems.Spawners
{
    /// <summary>
    /// A SpawnPoint is a prefab that is used for deciding the spawn locations of the players.
    /// </summary>
    public class SpawnPoint : NetworkActor
    {
        /// <summary>
        /// The data needed to validate the spawning conditions.
        /// For example, checking whether the player can spawn on this SpawnPoint based on their job (e.g. an assistant can't spawn on security SpawnPoint)
        /// </summary>
        public SpawnPointData SpawnPointData;

        /// <summary>
        /// Reserves a SpawnPoint so other players can't spawn on it once someone else has spawned on it.
        /// This only applies for Spawn Points that have SpawnType set to Job.
        /// </summary>
        public bool Reserved;

        public override void OnStartServer()
        {
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateChanged));
        }
        
        public void HandleRoundStateChanged(ref EventContext context, in RoundStateUpdated state)
        {
            // Prepare the SpawnPoint on the current map
            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
            if (state.RoundState == RoundState.WarmingUp && tileSubSystem.CurrentMap)
            {
                SubSystems.Get<SpawnPointManager>().RegisterSpawnPoint(this);
            }
        }

        public void Reserve()
        {
            Reserved = true;
        }
    }
}