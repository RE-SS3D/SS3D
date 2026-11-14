using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using SS3D.Logging;
using SS3D.Systems.Comms;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Health;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Combat;
using SS3D.Systems.Roles;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.SpawnPoints;
using SS3D.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Controls player spawning.
    /// </summary>
    public partial class EntitySubSystem : NetworkSubSystem
    {
        /// <summary>
        /// Event that should be evoked only on client, when the client spawns in the station.
        /// </summary>
        public Action OnClientSpawn;

        /// <summary>
        /// The prefab used for the player object.
        /// </summary>
        [Header("Settings")]
        [SerializeField]
        private List<Entity> _humanPrefab;

        /// <summary>
        /// The point used to place spawned players
        /// </summary>
        [SerializeField]
        private Transform _spawnPoint;

        /// <summary>
        /// List of the spawned players in the round
        /// </summary>
        [SyncObject]
        private readonly SyncList<Entity> _spawnedPlayers = new();

        /// <summary>Server-only: latest lobby character per ckey.</summary>
        private readonly Dictionary<string, CharacterAppearance> _appearances = new();

        /// <summary>Server-only: bodies placed per job this round, so a job's players spread over its markers.</summary>
        private readonly Dictionary<string, int> _jobSpawnTurns = new(StringComparer.Ordinal);

        /// <summary>
        /// If the system already spawned all the players that were ready when the round started
        /// </summary>
        [SyncVar(OnChange = nameof(SyncHasSpawnedInitialPlayers))]
        private bool _hasSpawnedInitialPlayers;

        public Entity GetSpawnedEntity(Player player)
        {
            var entity = _spawnedPlayers.Find(entity => entity != null && entity.Mind != null && entity.Mind.player == player);
            if (IsPlayerSpawned(player))
            {
                return entity;
            }
            return null;
        }

        public bool TryGetSpawnedEntity(NetworkConnection conn, out Entity entity)
        {
            entity = _spawnedPlayers.Find(entity => entity != null && entity.Mind?.player?.Owner == conn);
            return entity != null;
        }

        /// <summary>
        /// Returns true if the player is controlling an entity.
        /// </summary>
        /// <param name="playerThe player's ckey</param>
        /// <returns>Is the player is controlling an entity</returns>
        public bool IsPlayerSpawned(Player player)
        {
            Entity spawnedPlayer = _spawnedPlayers.Find(entity => entity != null && entity.Mind != null && entity.Mind.player == player);
            return spawnedPlayer != null && spawnedPlayer.Mind != Mind.Empty;
        }

        /// <summary>
        /// Returns true if the networkConnection is controlling an entity.
        /// </summary>
        /// <param name="networkConnection">The player's connection</param>
        /// <returns>Is the player is controlling an entity</returns>
        public bool IsPlayerSpawned(NetworkConnection networkConnection)
        {
            Entity spawnedPlayer = _spawnedPlayers.Find(entity =>
                entity != null && entity.Mind?.player?.Owner == networkConnection);

            bool isPlayerSpawned;

            if (spawnedPlayer == null)
            {
                isPlayerSpawned = false;
            }
            else if (spawnedPlayer.Mind == Mind.Empty)
            {
                isPlayerSpawned = false;
            }
            else
            {
                isPlayerSpawned = true;
            }

            return isPlayerSpawned;
        }

        /// <summary>
        /// Currently spawned players in the round. Do not mutate; SyncList is the source of truth.
        /// </summary>
        public IReadOnlyList<Entity> SpawnedPlayers => _spawnedPlayers;

        /// <summary>
        /// Server: a player's entity was added, removed or swapped (a death swaps the body for its ghost).
        /// Raised synchronously, so during a death the new ghost may not hold its mind yet.
        /// </summary>
        public event Action ServerSpawnedPlayersChanged;

        /// <summary>
        /// Returns the last spawned player.
        /// </summary>
        public Entity LastSpawned => _spawnedPlayers.Count != 0 ? _spawnedPlayers[^1] : null;

        protected override void OnStart()
        {
            base.OnStart();

            _spawnedPlayers.OnChange += HandleSpawnedPlayersChanged;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!TryGetComponent<HealthDebugController>(out _))
            {
                gameObject.AddComponent<HealthDebugController>();
            }

            if (!TryGetComponent<RespawnPrompt>(out _))
            {
                gameObject.AddComponent<RespawnPrompt>();
            }

            SyncSpawnedPlayers();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerManager.RegisterBroadcast<CharacterAppearanceMessage>(HandleCharacterAppearance);
            ServerAddEventListeners();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            ServerManager.UnregisterBroadcast<CharacterAppearanceMessage>(HandleCharacterAppearance);
        }

        /// <summary>
        /// Keeps the sender's latest lobby character per ckey for this server session; applied at
        /// their next spawn, never to a body that already exists.
        /// </summary>
        [Server]
        private void HandleCharacterAppearance(NetworkConnection sender, CharacterAppearanceMessage message)
        {
            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(sender);
            if (player == null)
            {
                return;
            }

            CharacterAppearance appearance = message.Appearance.Sanitized();
            // The creator only saves valid names; anything else came from an old or modified client. The body keeps
            // the look and goes by the player's ckey (Mind.DisplayName).
            if (!CharacterNameRules.TryNormalize(appearance.Name, out string name, out CharacterNameError error))
            {
                Log.Warning(this, "Refused character name from {ckey}: {error}", Logs.ServerOnly, player.Ckey, error);
            }

            appearance.Name = name;
            _appearances[player.Ckey] = appearance;
        }

        private void ServerAddEventListeners()
        {
            AddHandle(SpawnReadyPlayersEvent.AddListener(HandleSpawnReadyPlayers));
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
        }

        /// <summary>
        /// Asks the server to spawn a player.
        /// </summary>
        /// <param name="player</param>
        /// <param name="networkConnection"></param>
        [ServerRpc(RequireOwnership = false)]
        public void CmdSpawnLatePlayer(Player player, NetworkConnection networkConnection = null)
        {
            if (player == null || player.Owner != networkConnection) return;
            SpawnLatePlayer(player);
        }

        /// <summary>
        /// Asks the server to late-join the caller as <paramref name="job"/> (lobby effort Decision 11). The server
        /// re-checks the slot, first come first served, and tells the caller in OOC when it is gone.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CmdJoinAsJob(string job, NetworkConnection networkConnection = null)
        {
            Player player = SubSystems.Get<PlayerSubSystem>().GetPlayer(networkConnection);
            if (player == null) return;
            SpawnLatePlayer(player, job);
        }

        public bool TryGetOwnedEntity(NetworkConnection conn, out Entity entity)
        {
            foreach(Entity e in SpawnedPlayers)
            {
                if(e.Owner == conn)
                {
                    entity = e;
                    return true;
                }
            }
            entity = null;
            return false;
        }

        /// <summary>
        /// Reconnects a returning player to the body they were controlling before they disconnected.
        /// The body itself is never despawned on disconnect (see <see cref="HandleRoundStateUpdated"/>/
        /// <see cref="DestroySpawnedPlayers"/> - it only happens at round end), it's simply left ownerless
        /// in the world. This re-links it to the new connection instead of leaving it stranded and the
        /// reconnecting player stuck without a controllable entity.
        /// </summary>
        [Server]
        public bool TryReclaimEntity(Player player, NetworkConnection conn)
        {
            Entity entity = _spawnedPlayers.Find(e => e != null && e.Mind?.player == player);
            if (entity == null)
            {
                return false;
            }

            entity.GiveOwnership(conn);
            entity.Mind?.GiveOwnership(conn);
            ServerSetFirstObjectAndRebuildObservers(conn, entity.NetworkObject);

            RpcInvokeClientSpawned(entity.Owner);

            Log.Information(this, "Reconnected {ckey} to their existing body {entity}", Logs.ServerOnly, player.Ckey, entity.name);

            return true;
        }

        /// <summary>
        /// Spawns a mindless Human for combat/interaction testing. Server-owned; no mind or loadout.
        /// </summary>
        [Server]
        public Entity ServerSpawnCombatDummy(Vector3 position, Quaternion rotation)
        {
            if (_humanPrefab == null || _humanPrefab.Count == 0)
            {
                Log.Error(this, "No human prefab configured on EntitySubSystem", Logs.ServerOnly);
                return null;
            }

            Entity prefab = _humanPrefab[0];
            Entity entity = Instantiate(prefab, position, rotation);
            ServerManager.Spawn(entity.NetworkObject);

            if (!entity.TryGetComponent(out CombatDummyBootstrap bootstrap))
            {
                bootstrap = entity.gameObject.AddComponent<CombatDummyBootstrap>();
            }

            bootstrap.ConfigureAsDummy();
            bootstrap.EquipArmorTestLoadout();

            Log.Information(this, "Spawned combat dummy at {position}", Logs.ServerOnly, position);
            return entity;
        }

        /// <summary>
        /// Spawns a player after the round has started, as <paramref name="pickedJob"/> when given (Decision 11),
        /// else as their character's highest ranked open job.
        /// </summary>
        /// <param name="player">The latejoiner.</param>
        /// <param name="pickedJob">The job picked on the Join screen; null resolves the character's priorities.</param>
        [Server]
        private void SpawnLatePlayer(Player player, string pickedJob = null)
        {
            if (SubSystems.Get<RoundSubSystem>().CurrentRoundState != RoundState.Ongoing
                || !SubSystems.Get<ServerSettingsService>().AllowLateJoin)
            {
                return;
            }

            if (!IsPlayerSpawned(player) && _hasSpawnedInitialPlayers)
            {
                RoleSubSystem roles = SubSystems.Get<RoleSubSystem>();
                if (pickedJob != null)
                {
                    if (!roles.TryAssignLateJoinPick(player, pickedJob, out string refusal))
                    {
                        SubSystems.GetOrNull<CommsSubSystem>()?.SendOocNoticeTo(player.Owner, refusal);
                        return;
                    }
                }
                else if (!roles.TryAssignLateJoinRole(player))
                {
                    SubSystems.GetOrNull<CommsSubSystem>()?.SendOocNoticeTo(player.Owner, RoleSubSystem.ReturnedToLobbyNotice);
                    return;
                }

                Entity entity = SpawnPlayer(player);

                string job = SubSystems.Get<RoleSubSystem>().TryGetRole(player, out RoleData role)
                    ? role.Name.ToLowerInvariant()
                    : "crew";
                SubSystems.GetOrNull<CommsSubSystem>()?.SendAnnouncement(
                    $"{entity.Mind.DisplayName}, {job}, has joined the ship");
            }
        }

        /// <summary>
        /// Spawns a player with a Ckey
        /// </summary>
        /// <param name="playerUnique user object</param>
        /// <param name="existingMind">A respawning player's mind, moved from their ghost; null creates one.</param>
        [Server]
        private Entity SpawnPlayer(Player player, Mind existingMind = null)
        {
            Mind createdMind = existingMind;
            if (createdMind == null)
            {
                SubSystems.Get<MindSubSystem>().TryCreateMind(player, out createdMind);
            }

            Vector3 spawnPosition = ResolveSpawnPosition(player);
            Entity entity = Instantiate(_humanPrefab[Random.Range(0, _humanPrefab.Count)], spawnPosition, Quaternion.identity);
            ServerManager.Spawn(entity.NetworkObject, player.Owner);

            createdMind.SetPlayer(player);
            if (_appearances.TryGetValue(player.Ckey, out CharacterAppearance appearance))
            {
                createdMind.CharacterName = appearance.Name;
                if (entity is Human human)
                {
                    human.SetAppearance(appearance);
                }
            }

            entity.SetMind(createdMind);

            player.Owner.SetFirstObject(entity.NetworkObject);
            ServerRebuildObservers(player.Owner);

            SubSystems.Get<RoleSubSystem>().GiveRoleLoadoutToPlayer(entity);

            _spawnedPlayers.Add(entity);

            RpcInvokeClientSpawned(entity.Owner);

            Log.Information(this, "Spawning mind {createdMind} on {entity}", Logs.ServerOnly, createdMind.name, entity.name);
            return entity;
        }

        [Server]
        private void ServerSetFirstObjectAndRebuildObservers(NetworkConnection conn, NetworkObject networkObject)
        {
            if (conn == null || !conn.IsValid || networkObject == null)
            {
                return;
            }

            conn.SetFirstObject(networkObject);
            ServerRebuildObservers(conn);
        }

        /// <summary>
        /// Warm HashGrid AOI immediately after FirstObject is set. Waiting for Entity.Update's
        /// cell-cross interval leaves early Embark clients with an empty tilemap.
        /// </summary>
        [Server]
        private void ServerRebuildObservers(NetworkConnection conn)
        {
            if (conn == null || !conn.IsValid || ServerManager?.Objects == null)
            {
                return;
            }

            ServerManager.Objects.RebuildObservers(conn, timedOnly: false);
        }

        /// <summary>
        /// Operatives spawn on the map's Nuclear Operatives markers, spread over them in draw order.
        /// Crew with a job spawn on that job's markers, taking them in turn. Everyone else (or a job with no
        /// marker): the legacy inspector spawn transform; otherwise the first authored map spawn
        /// marker; otherwise the walkable deck nearest the hub transform (a map that has lost its markers, started
        /// with <see cref="RoundAdminAction.EditMapAnyway"/> to fix it); otherwise the hub transform itself (scene
        /// spawn points are gone after Phase 3h).
        /// </summary>
        private const int NoMarkerSearchRadius = 64;

        private Vector3 ResolveSpawnPosition(Player player)
        {
            if (TryResolveOperativeSpawn(player, out Vector3 operativeSpawn))
            {
                return operativeSpawn;
            }

            if (TryResolveJobSpawn(player, out Vector3 jobSpawn))
            {
                return jobSpawn;
            }

            if (_spawnPoint != null)
            {
                return _spawnPoint.position;
            }

            if (SubSystems.TryGet(out TileSubSystem tile)
                && tile.SpawnPoints != null
                && tile.SpawnPoints.Count > 0)
            {
                return tile.SpawnPoints.Records[0].Position;
            }

            if (StationBoundsRescue.TryFindWalkableDeck(transform.position, NoMarkerSearchRadius, out Vector3 deck))
            {
                return deck;
            }

            return transform.position;
        }

        private bool TryResolveJobSpawn(Player player, out Vector3 position)
        {
            position = default;
            if (!SubSystems.TryGet(out RoleSubSystem roles) || !roles.TryGetRole(player, out RoleData role)
                || role == null || role.IsAntagonist
                || !SubSystems.TryGet(out TileSubSystem tile) || tile.SpawnPoints == null)
            {
                return false;
            }

            _jobSpawnTurns.TryGetValue(role.Name, out int turn);
            if (!tile.SpawnPoints.TryGetJobSpawn(role.Name, turn, out position))
            {
                return false;
            }

            _jobSpawnTurns[role.Name] = turn + 1;
            return true;
        }

        private static bool TryResolveOperativeSpawn(Player player, out Vector3 position)
        {
            position = default;
            if (!SubSystems.TryGet(out RoleSubSystem roles) || !roles.IsOperative(player)
                || !SubSystems.TryGet(out TileSubSystem tile) || tile.SpawnPoints == null)
            {
                return false;
            }

            List<SpawnPointRecord> markers = tile.SpawnPoints.Records
                .Where(record => record.Kind == SpawnPointKind.Antagonist
                    && record.AntagonistCategory == AntagonistSpawnCategory.NuclearOperatives)
                .ToList();
            if (markers.Count == 0)
            {
                return false;
            }

            int index = 0;
            for (int i = 0; i < roles.Operatives.Count; i++)
            {
                if (roles.Operatives[i] == player)
                {
                    index = i;
                    break;
                }
            }

            position = markers[index % markers.Count].Position;
            return true;
        }

        /// <summary>
        /// Spawns all the players that are ready when the round starts. Players who observed during the warmup
        /// already have their ghost and are left out by <see cref="ReadyPlayersSubSystem"/>.
        /// </summary>
        [Server]
        private void SpawnReadyPlayers(List<Player> players)
        {
            if (_hasSpawnedInitialPlayers) return;

            if (players.Count == 0)
            {
                Log.Information(this, "No players to spawn", Logs.ServerOnly);
            }

            IReadOnlyList<Player> stayInLobby = SubSystems.Get<RoleSubSystem>().ResolveRoundStartRoles(players);

            foreach (Player ckey in players)
            {
                if (stayInLobby.Contains(ckey))
                {
                    SubSystems.GetOrNull<CommsSubSystem>()?.SendOocNoticeTo(ckey.Owner, RoleSubSystem.ReturnedToLobbyNotice);
                    continue;
                }

                SpawnPlayer(ckey);
            }

            _hasSpawnedInitialPlayers = true;

            new InitialPlayersSpawned(new List<Entity>(_spawnedPlayers)).Invoke(this);
        }

        /// <summary>
        /// Destroys all spawned players
        /// </summary>
        [Server]
        private void DestroySpawnedPlayers()
        {
            var snapshot = new List<Entity>(_spawnedPlayers);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Entity player = snapshot[i];
                if (player == null)
                    continue;

                // Every spawn creates a Mind and nothing else despawns it; without this one leaked per player per round.
                Mind mind = player.Mind;
                if (mind != null && mind != Mind.Empty && mind.IsSpawned)
                {
                    ServerManager.Despawn(mind.NetworkObject);
                }

                ServerManager.Despawn(player.NetworkObject);
                player.GameObject.Dispose(true);
            }

            _hasSpawnedInitialPlayers = false;
            _jobSpawnTurns.Clear();
            _spawnedPlayers.Clear();
            _respawnCooldown.Clear();
            _observerCkeys.Clear();
            DespawnCorpses();
        }

        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            RoundState roundState = e.RoundState;

            if (roundState != RoundState.Stopped)
            {
                return;
            }

            DestroySpawnedPlayers();
        }

        [Server]
        private void HandleSpawnReadyPlayers(ref EventContext context, in SpawnReadyPlayersEvent e)
        {
            SpawnReadyPlayers(e.ReadyPlayers);
        }

        private void HandleSpawnedPlayersChanged(SyncListOperation op, int index, Entity old, Entity @new, bool asServer)
        {
            if (op == SyncListOperation.Complete)
            {
                return;
            }

            if (asServer)
            {
                ServerSpawnedPlayersChanged?.Invoke();
            }

			if(op == SyncListOperation.Set)
			{
				return;
			}

            if (!asServer && IsHost)
            {
                return;
            }

            SyncSpawnedPlayers();
        }

        private void SyncSpawnedPlayers()
        {
            if (_spawnedPlayers.Count == 0)
            {
                return;
            }

            SpawnedPlayersUpdated spawnedPlayersUpdated = new(new List<Entity>(_spawnedPlayers));
            spawnedPlayersUpdated.Invoke(this);
        }

        private void SyncHasSpawnedInitialPlayers(bool oldValue, bool newValue, bool asServer)
        {
            if (!asServer && IsHost)
            {
                return;
            }
        }

		public bool TryTransferEntity(Entity oldEntity, Entity newEntity)
		{
			int index = _spawnedPlayers.FindIndex(x => x == oldEntity);
            if (index == -1)
            {
                Log.Warning(this, $"could not find entity {oldEntity} in the list of spawned entity controlled by players");
                return false;
            }
			_spawnedPlayers[index] = newEntity;
            return true;
		}

        [TargetRpc]
        private void RpcInvokeClientSpawned(NetworkConnection target)
        {
            OnClientSpawn?.Invoke();
        }


	}
}
