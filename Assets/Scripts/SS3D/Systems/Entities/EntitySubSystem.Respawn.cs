using System;
using System.Collections.Generic;
using Coimbra;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Per-player respawn cooldown, counted from death (<c>death-cloning-respawn.md</c> §6).
    /// Times are in seconds on any monotonic clock.
    /// </summary>
    public sealed class RespawnCooldown
    {
        private readonly Dictionary<string, double> _deaths = new(StringComparer.Ordinal);

        public void RecordDeath(string ckey, double now)
        {
            if (!string.IsNullOrEmpty(ckey))
                _deaths[ckey] = now;
        }

        /// <summary>Whole seconds until <paramref name="ckey"/> may respawn; 0 when allowed or never died.</summary>
        public int SecondsLeft(string ckey, double now, int cooldownSeconds)
        {
            if (string.IsNullOrEmpty(ckey) || !_deaths.TryGetValue(ckey, out double died))
                return 0;

            return Math.Max(0, (int)Math.Ceiling(died + cooldownSeconds - now));
        }

        public void Forget(string ckey) => _deaths.Remove(ckey);

        public void Clear() => _deaths.Clear();
    }

    public static class RespawnRules
    {
        /// <summary>Why a respawn is refused, or null when it is allowed.</summary>
        public static string Refusal(bool enabled, bool roundOngoing, bool isGhost, int secondsLeft)
        {
            if (!enabled) return "Respawn is off on this server.";
            if (!roundOngoing) return "You can only respawn during a round.";
            if (!isGhost) return "Only ghosts can respawn.";
            return secondsLeft > 0 ? $"You can respawn in {secondsLeft} seconds." : null;
        }
    }

    /// <summary>
    /// Respawn (<c>sandbox-server.md</c> §4): a ghost asks the server for a fresh body. The server checks
    /// <see cref="ServerSettings.RespawnEnabled"/> and the cooldown, despawns the ghost and runs the normal
    /// spawn path, so the player always gets a new body at a spawn point. The sandbox profile removes the previous corpse.
    /// </summary>
    public partial class EntitySubSystem
    {
        private readonly RespawnCooldown _respawnCooldown = new();

        /// <summary>
        /// Bodies left behind by dead players. They leave <see cref="_spawnedPlayers"/> when the ghost takes
        /// over, so round end despawns them from here, or every death would leak a body into the next round.
        /// </summary>
        private readonly List<Entity> _corpses = new();
        private readonly Dictionary<string, Entity> _previousCorpseByPlayer = new(StringComparer.Ordinal);

        /// <summary>Client: fired when the server tells the local player when they may respawn (seconds from now).</summary>
        public event Action<int> LocalRespawnOffered;

        /// <summary>Client: fired when the local player respawned or the offer no longer applies.</summary>
        public event Action LocalRespawnWithdrawn;

        private static double Now => Time.realtimeSinceStartupAsDouble;

        private static ServerSettings Settings => SubSystems.Get<RoundSubSystem>().SettingsForServer;

        /// <summary>Server: a player's body died and their ghost took over; starts the cooldown and offers respawn.</summary>
        [Server]
        public void RecordPlayerDeath(Entity corpse, Entity ghost)
        {
            if (corpse != null)
                _corpses.Add(corpse);

            Player player = ghost != null && ghost.Mind != null ? ghost.Mind.player : null;
            if (player == null)
                return;

            if (corpse != null)
                _previousCorpseByPlayer[player.Ckey] = corpse;

            _respawnCooldown.RecordDeath(player.Ckey, Now);
            ServerSettings settings = Settings;
            if (settings.RespawnEnabled && player.Owner != null && player.Owner.IsActive)
                TargetRespawnOffered(player.Owner, settings.RespawnCooldownSeconds);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdRespawn(NetworkConnection conn = null) => TryRespawn(conn, out _);

        /// <summary>Server: replaces the caller's ghost with a fresh body; <paramref name="refusal"/> says why not.</summary>
        [Server]
        public bool TryRespawn(NetworkConnection conn, out string refusal)
        {
            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(conn);
            TryGetSpawnedEntity(conn, out Entity current);
            bool isGhost = player != null && current != null && current.GetComponent<HumanoidGhostController>() != null;
            ServerSettings settings = Settings;
            bool ongoing = SubSystems.Get<RoundSubSystem>().CurrentRoundState == RoundState.Ongoing;
            int secondsLeft = player == null ? 0 : _respawnCooldown.SecondsLeft(player.Ckey, Now, settings.RespawnCooldownSeconds);

            refusal = RespawnRules.Refusal(settings.RespawnEnabled, ongoing, isGhost, secondsLeft);
            if (refusal != null)
            {
                Log.Information(this, "Respawn refused for {ckey}: {reason}", Logs.ServerOnly, player?.Ckey, refusal);
                if (conn != null && conn.IsActive)
                    TargetRespawnRefused(conn, secondsLeft);
                return false;
            }

            Mind mind = current.Mind;
            _spawnedPlayers.Remove(current);
            current.SetMind(null);
            ServerManager.Despawn(current.NetworkObject);
            current.GameObject.Dispose(true);
            _respawnCooldown.Forget(player.Ckey);

            Entity body = SpawnPlayer(player, mind);
            if (body != null && _previousCorpseByPlayer.Remove(player.Ckey, out Entity corpse)
                && settings.RemoveCorpseOnRespawn && corpse != null)
            {
                _corpses.Remove(corpse);
                if (corpse.IsSpawned)
                    ServerManager.Despawn(corpse.NetworkObject);
                corpse.GameObject.Dispose(true);
            }
            Log.Information(this, "Respawned {ckey} as {entity}", Logs.ServerOnly, player.Ckey, body != null ? body.name : "nothing");
            TargetRespawnWithdrawn(conn);
            return body != null;
        }

        /// <summary>Server, round end: despawns every corpse still in the world.</summary>
        [Server]
        private void DespawnCorpses()
        {
            foreach (Entity corpse in _corpses)
            {
                if (corpse == null || !corpse.IsSpawned)
                    continue;

                ServerManager.Despawn(corpse.NetworkObject);
                corpse.GameObject.Dispose(true);
            }

            _corpses.Clear();
            _previousCorpseByPlayer.Clear();
        }

        [TargetRpc]
        private void TargetRespawnOffered(NetworkConnection conn, int secondsFromNow) => LocalRespawnOffered?.Invoke(secondsFromNow);

        [TargetRpc]
        private void TargetRespawnRefused(NetworkConnection conn, int secondsLeft)
        {
            if (secondsLeft > 0)
                LocalRespawnOffered?.Invoke(secondsLeft);
        }

        [TargetRpc]
        private void TargetRespawnWithdrawn(NetworkConnection conn) => LocalRespawnWithdrawn?.Invoke();
    }
}
