using System.Collections.Generic;
using SS3D.Core.Behaviours;
using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.GameModes.Modes;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using UnityEngine;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Roles;
using SS3D.Systems.Comms;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// Controls the gamemode that the round will use. Does all the networking magic.
    /// </summary>
    public sealed class GamemodeSubSystem : NetworkSubSystem
    {
        /// <summary>
        /// The gamemode that is being used.
        /// </summary>
        [SerializeField]
        private Gamemode _gamemode;

        /// <summary>
        /// Antagonist list in the round. Currently unused.
        /// </summary>
        public List<string> Antagonists => _gamemode.RoundAntagonists;

        public string CurrentGamemodeName
        {
            get
            {
                if (IsServer && SubSystems.TryGet(out RoundSubSystem rounds)
                    && rounds.IsExtendedRound) return "Extended";
                return _gamemode != null ? _gamemode.name : "unknown";
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            if (base.IsServer) Setup();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            CmdGetCurrentClientObjectives();
        }

        /// <summary>
        /// Prepares the gamemode.
        /// </summary>
        [Server]
        private void Setup()
        {   
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
            AddHandle(InitialPlayersSpawned.AddListener(HandleInitialPlayersSpawned));
        }

        /// <summary>
        /// Initializes the gamemode: the drawn squad gets its team objective and a briefing. Crew and
        /// latejoiners hold no objective (Decision 13).
        /// </summary>
        [Server]
        private void InitializeGamemode()
        {
            if (SubSystems.Get<RoundSubSystem>().IsExtendedRound) return;
            // Secret currently resolves to the sole available antagonist mode.
            // Creates an instance of the SO, to avoid using the file. 
            _gamemode = Instantiate(_gamemode);

            // Subscribe to Gamemode events
            _gamemode.OnInitialized += HandleGamemodeInitialized;
            _gamemode.OnFinished += HandleGamemodeFinalized;

            IReadOnlyList<Player> squad = SubSystems.Get<RoleSubSystem>().Operatives;
            List<string> squadCkeys = squad.Select(player => player.Ckey).ToList();

            // Actually initialize the gamemode
            _gamemode.InitializeGamemode(squadCkeys);

            // Add event listeners
            foreach (var objective in _gamemode.RoundObjectives)
            {
                objective.AddEventListeners();
            }

            // Subscribe to Objective events - we cannot do this before initializing the gamemode.
            _gamemode.OnObjectiveUpdated += HandleObjectiveUpdated;

            // Update all clients with the objectives
            foreach (GamemodeObjective gamemodeObjective in _gamemode.RoundObjectives)
            {
                SendObjectiveToClients(gamemodeObjective);
            }

            SendBriefingsAsync(squad).Forget();
        }

        /// <summary>Seconds after spawn before the briefing is sent, so the client's HUD feed exists to show it.</summary>
        private const float BriefingDelaySeconds = 3f;

        /// <summary>
        /// A private feed message to each operative naming their role, objective and squadmates
        /// (<c>objectives.md</c> §8, Decision 14).
        /// </summary>
        [Server]
        private async UniTaskVoid SendBriefingsAsync(IReadOnlyList<Player> squad)
        {
            if (squad.Count == 0 || !SubSystems.TryGet(out EntitySubSystem entities))
            {
                return;
            }

            Dictionary<Player, string> names = new();
            foreach (Entity entity in entities.SpawnedPlayers)
            {
                Player player = entity != null && entity.Mind != null ? entity.Mind.player : null;
                if (player != null && squad.Contains(player))
                {
                    names[player] = entity.Mind.DisplayName;
                }
            }

            List<Player> members = squad.ToList();
            await UniTask.Delay(TimeSpan.FromSeconds(BriefingDelaySeconds), ignoreTimeScale: true);
            if (!SubSystems.TryGet(out CommsSubSystem comms) || SubSystems.Get<RoundSubSystem>().CurrentRoundState != RoundState.Ongoing)
            {
                return;
            }

            foreach (Player member in members)
            {
                if (member == null || member.Owner == null || !member.Owner.IsActive)
                {
                    continue;
                }

                string role = SubSystems.Get<RoleSubSystem>().TryGetRole(member, out RoleData data) ? data.Name : "Operative";
                List<GamemodeObjective> objectives = _gamemode?.GetPlayerObjectives(member.Ckey) ?? new List<GamemodeObjective>();
                IEnumerable<string> squadmates = members.Where(other => other != member)
                    .Select(other => names.TryGetValue(other, out string name) ? name : other.Ckey);
                comms.SendPrivateFeed(member.Owner, OperativeBriefing.Header,
                    OperativeBriefing.Text(role, objectives.Select(objective => objective.Title), squadmates));
            }
        }

        /// <summary>
        /// Resolves the objectives at the round's end trigger and returns them as the summary lists them.
        /// Empty in a round without a gamemode (Extended).
        /// </summary>
        [Server]
        public IEnumerable<RoundOutcomeObjective> ResolveObjectives(RoundEndRequest request)
        {
            if (_gamemode == null || !_gamemode.IsInitialized)
            {
                return Array.Empty<RoundOutcomeObjective>();
            }

            _gamemode.ResolveObjectives(request);
            return _gamemode.OutcomeObjectives();
        }

        /// <summary>
        /// Finalizes the gamemode.
        /// </summary>
        [Server]
        private void FinalizeGamemode()
        {
            if (_gamemode == null || !_gamemode.IsInitialized)
            {
                return;
            }

            _gamemode.OnInitialized -= HandleGamemodeInitialized;
            _gamemode.OnFinished -= HandleGamemodeFinalized;
            _gamemode.OnObjectiveUpdated -= HandleObjectiveUpdated;
            
            _gamemode.FinalizeGamemode();
            _gamemode.ResetGamemode();

            // Removes the current gamemode.
            // _gamemode = null;
        }

        /// <summary>
        /// The gamemode's end condition fired (<c>round-end.md</c> §2) with no winner of its own.
        /// </summary>
        [Server]
        public void EndRound() => EndRound(new RoundEndRequest(RoundEndTrigger.GamemodeCondition));

        /// <summary>The gamemode's end condition fired; the request carries its winner and wording.</summary>
        [Server]
        public void EndRound(RoundEndRequest request)
        {
            SubSystems.Get<RoundSubSystem>().RequestRoundEnd(request);
        }

        /// <summary>
        /// Sends an objective to all interested clients. Usually the owner and admins.
        /// </summary>
        /// <param name="objective">The objective to be sent via network message.</param>
        [Server]
        private void SendObjectiveToClients(GamemodeObjective objective)
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();

            Player assignee = playerSystem.GetPlayer(objective.AssigneeCkey);
            if (assignee == null)
            {
                return;
            }

            NetworkConnection author = assignee.Owner;
            GamemodeObjectiveUpdatedMessage message = new(objective);

            // TODO Add admins as receivers of this message
            HashSet<NetworkConnection> receivers = new()
            {
                author,
                // TODO: Get admins or something 🥸
            };

            ServerManager.Broadcast(receivers, message);
        }

        /// <summary>
        /// Asks the server for the current GamemodeObjectives for a client that joined late.
        /// </summary>
        /// <param name="sender">Who asked the objectives.</param>
        [ServerRpc(RequireOwnership = false)]
        private void CmdGetCurrentClientObjectives(NetworkConnection sender = null)
        {
            if (_gamemode == null)
            {
                return;
            }

            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();

            List<GamemodeObjective> gamemodeObjectives = _gamemode.GetPlayerObjectives(playerSystem.GetCkey(sender));

            if (gamemodeObjectives == null)
            {
                return;
            }

            foreach (GamemodeObjective gamemodeObjective in gamemodeObjectives)
            {
                SendObjectiveToClients(gamemodeObjective);
            }
        }

        /// <summary>
        /// Called whenever the ready players are spawned at the start of the round.          
        /// </summary>
        [Server]
        private void HandleInitialPlayersSpawned(ref EventContext context, in InitialPlayersSpawned e)
        {
            InitializeGamemode();   
        }

        /// <summary>
        /// Called whenever the round state is updated.
        /// </summary>
        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            switch (e.RoundState)
            {
                case RoundState.Ending:
                    FinalizeGamemode();
                    break;
            }
        }

        /// <summary>
        /// Called once the gamemode was finished.
        /// </summary>
        /// <param name="roundObjectives">The objectives on the finished round.</param>
        [Server]
        private void HandleGamemodeFinalized(List<GamemodeObjective> roundObjectives)
        {
             // TODO: Send clients all objectives in that round.
        }

        /// <summary>
        /// Called once the gamemode is initialized.
        /// </summary>
        [Server]
        private void HandleGamemodeInitialized()
        {
            // Not sure yet. Probably no logic will run here.
        }

        /// <summary>
        /// Called when an objective is updated, sends it to the client.
        /// </summary>
        /// <param name="objective">The objective to be sent.</param>
        [Server]
        private void HandleObjectiveUpdated(GamemodeObjective objective)
        {
            SendObjectiveToClients(objective);
        }
    }
}