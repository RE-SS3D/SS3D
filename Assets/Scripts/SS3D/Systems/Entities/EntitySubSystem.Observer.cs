using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Server rules for the lobby's Observe button (<c>observer.md</c> §2, <c>lobby.md</c> §7). Observing is immediate,
    /// before or during the round, but only while a station is loaded (lobby effort Decision 9): during the warmup
    /// before the round starts and while it runs. The lobby countdown runs with no station, so it can't be observed.
    /// </summary>
    public static class ObserveRules
    {
        /// <summary>The round states with a loaded station a ghost can enter.</summary>
        public static bool StationLoaded(RoundState state, StationLoadState load) =>
            (state is RoundState.WarmingUp or RoundState.Ongoing) && load == StationLoadState.Ready;

        /// <summary>Why an observe request is refused, or null when it is allowed.</summary>
        public static string Refusal(bool stationLoaded, bool hasEntity)
        {
            if (!stationLoaded) return "You can only observe once the station is loaded.";
            return hasEntity ? "You are already in the round." : null;
        }
    }

    /// <summary>
    /// Observer mode (<c>observer.md</c> §2 first pass): a player who chose Observe enters the round as a
    /// ghost with no body behind it, straight away, and is taken off the ready list. An observer can never
    /// respawn or late-join that round; round end returns them to the lobby like everyone else.
    /// </summary>
    public partial class EntitySubSystem
    {
        /// <summary>Server: ckeys observing the current round. Cleared at round end.</summary>
        private readonly HashSet<string> _observerCkeys = new(StringComparer.Ordinal);

        /// <summary>Server: true when <paramref name="player"/> entered this round as an observer.</summary>
        [Server]
        public bool IsObserver(Player player) => player != null && _observerCkeys.Contains(player.Ckey);

        /// <summary>Asks the server to put the caller in the round as an observer ghost, now.</summary>
        [ServerRpc(RequireOwnership = false)]
        public void CmdObserve(NetworkConnection conn = null) => TryObserve(conn, out _);

        /// <summary>Server: spawns an observer ghost for <paramref name="conn"/>; <paramref name="refusal"/> says why not.</summary>
        [Server]
        public bool TryObserve(NetworkConnection conn, out string refusal)
        {
            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(conn);
            if (player == null)
            {
                refusal = "Unknown player.";
                return false;
            }

            RoundSubSystem rounds = SubSystems.Get<RoundSubSystem>();
            bool hasEntity = _spawnedPlayers.FindIndex(e => e != null && e.Mind != null && e.Mind.player == player) >= 0;
            refusal = ObserveRules.Refusal(
                ObserveRules.StationLoaded(rounds.CurrentRoundState, rounds.CurrentStationLoadState), hasEntity);
            if (refusal != null)
            {
                Log.Information(this, "Observe refused for {ckey}: {reason}", Logs.ServerOnly, player.Ckey, refusal);
                return false;
            }

            SubSystems.GetOrNull<ReadyPlayersSubSystem>()?.Unready(player);
            return SpawnObserver(player) != null;
        }

        /// <summary>
        /// Server: spawns a ghost owned by <paramref name="player"/> at the crew spawn and gives it a new mind.
        /// The ghost is the same prefab a dying body leaves behind, so it moves and is hidden the same way.
        /// </summary>
        [Server]
        private Entity SpawnObserver(Player player)
        {
            GameObject ghostPrefab = ResolveGhostPrefab();
            if (ghostPrefab == null)
            {
                Log.Error(this, "No ghost prefab on the human prefab; cannot spawn an observer", Logs.ServerOnly);
                return null;
            }

            if (!SubSystems.Get<MindSubSystem>().TryCreateMind(player, out Mind mind))
            {
                return null;
            }

            Vector3 position = ResolveSpawnPosition(player);
            GameObject ghostObject = Instantiate(ghostPrefab, position, Quaternion.identity);
            Entity ghost = ghostObject.GetComponent<Entity>();
            ServerManager.Spawn(ghostObject, player.Owner);

            mind.SetPlayer(player);
            ghost.SetMind(mind);

            _observerCkeys.Add(player.Ckey);
            ServerSetFirstObjectAndRebuildObservers(player.Owner, ghost.NetworkObject);
            _spawnedPlayers.Add(ghost);

            RpcInvokeClientSpawned(ghost.Owner);

            Log.Information(this, "{ckey} is observing as {entity}", Logs.ServerOnly, player.Ckey, ghost.name);
            return ghost;
        }

        private GameObject ResolveGhostPrefab()
        {
            if (_humanPrefab == null)
            {
                return null;
            }

            foreach (Entity prefab in _humanPrefab)
            {
                if (prefab is Human human && human.Ghost != null)
                {
                    return human.Ghost;
                }
            }

            return null;
        }
    }
}
