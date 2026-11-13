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
    /// <summary>Server rules for the lobby's Observe button (<c>observer.md</c> §2, <c>lobby.md</c> §7).</summary>
    public static class ObserveRules
    {
        /// <summary>Why a mid-round observe request is refused, or null when it is allowed.</summary>
        public static string Refusal(bool roundOngoing, bool hasEntity)
        {
            if (!roundOngoing) return "You can only observe a running round.";
            return hasEntity ? "You are already in the round." : null;
        }
    }

    /// <summary>
    /// Observer mode (<c>observer.md</c> §2 first pass): a player who chose Observe enters the round as a
    /// ghost with no body behind it. Players who chose it in the lobby become ghosts when the round starts;
    /// during a round the button makes one straight away. An observer can never respawn or late-join that
    /// round; round end returns them to the lobby like everyone else.
    /// </summary>
    public partial class EntitySubSystem
    {
        /// <summary>Server: ckeys observing the current round. Cleared at round end.</summary>
        private readonly HashSet<string> _observerCkeys = new(StringComparer.Ordinal);

        /// <summary>Server: true when <paramref name="player"/> entered this round as an observer.</summary>
        [Server]
        public bool IsObserver(Player player) => player != null && _observerCkeys.Contains(player.Ckey);

        /// <summary>Asks the server to put the caller in the running round as an observer ghost.</summary>
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

            bool ongoing = SubSystems.Get<RoundSubSystem>().CurrentRoundState == RoundState.Ongoing;
            bool hasEntity = _spawnedPlayers.FindIndex(e => e != null && e.Mind != null && e.Mind.player == player) >= 0;
            refusal = ObserveRules.Refusal(ongoing && _hasSpawnedInitialPlayers, hasEntity);
            if (refusal != null)
            {
                Log.Information(this, "Observe refused for {ckey}: {reason}", Logs.ServerOnly, player.Ckey, refusal);
                return false;
            }

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
