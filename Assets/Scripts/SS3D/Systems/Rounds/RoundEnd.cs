using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Comms;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Roles;
using SS3D.Systems.Rounds.Messages;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// The round-end loop (<c>round-end.md</c> §2, §5, §6; effort <c>2026-11_round-end-loop</c> P1): every
    /// trigger goes through <see cref="RequestRoundEnd"/>, which fixes a <see cref="RoundOutcome"/> on the
    /// spot. Ending shows it to everyone for <see cref="RoundEndBeat.Seconds"/> (an admin can hold the beat),
    /// then the round stops as before. Clients keep the outcome as the lobby's Last round until the next one.
    /// </summary>
    public sealed partial class RoundSubSystem
    {
        private readonly RoundEndBeat _endBeat = new();
        /// <summary>Beat length for the next round end; only automation changes it (<c>SetEndBeatSecondsForTesting</c>).</summary>
        private int _endBeatSeconds = RoundEndBeat.Seconds;
        private RoundOutcome _pendingOutcome;
        /// <summary>Set by the squad draw (<see cref="FallBackToExtended"/>) when Secret/Nuclear Emergency ran as Extended.</summary>
        private bool _roundFellBackToExtended;

        [SyncVar] private int _endSecondsLeft;
        [SyncVar] private bool _endHeld;

        /// <summary>Client: the last round's outcome, kept until the next round ends. Null before any round ended.</summary>
        public RoundOutcome LastOutcome { get; private set; }

        /// <summary>Client: a new outcome arrived (a round just ended, or on connecting after one did).</summary>
        public event Action<RoundOutcome> OutcomeReceived;

        /// <summary>Seconds left on the summary beat; frozen while <see cref="EndHeld"/>.</summary>
        public int EndSecondsLeft => _endSecondsLeft;

        /// <summary>An admin is holding the summary beat.</summary>
        public bool EndHeld => _endHeld;

        /// <summary>Server: this round runs without antagonists, configured as Extended or fallen back to it.</summary>
        public bool IsExtendedRound => _serverSettings.Mode == "Extended" || _roundFellBackToExtended;

        /// <summary>
        /// The squad draw found too few opt-ins (Decision 11): the round runs as Extended, and admins hear
        /// why in Admin chat and the Server Settings status. Round start only.
        /// </summary>
        [Server]
        public void FallBackToExtended(string notice)
        {
            _roundFellBackToExtended = true;
            _serverSettingsError = notice;
            Log.Warning(this, notice, Logs.ServerOnly);
            if (SubSystems.TryGet(out CommsSubSystem comms))
                comms.PostOocSystem(OocChannel.Admin, notice);
        }

        /// <summary>
        /// The single entry point for every round end (gamemode condition, timer, admin). The outcome is
        /// fixed here, at the trigger instant; nothing after this changes it. Ignored unless Ongoing.
        /// </summary>
        [Server]
        public void RequestRoundEnd(RoundEndRequest request)
        {
            if (!IsOngoing || _pendingOutcome != null)
                return;

            _pendingOutcome = BuildOutcome(request);
            RequestRoundStateChange(new ChangeRoundStateMessage(false));
        }

        [Server]
        private RoundOutcome BuildOutcome(RoundEndRequest request)
        {
            bool hasGamemodes = SubSystems.TryGet(out GamemodeSubSystem gamemodes);
            string mode = hasGamemodes ? gamemodes.CurrentGamemodeName : "";
            // Objectives resolve here, at the trigger instant, like everything else on the outcome.
            IEnumerable<RoundOutcomeObjective> objectives = hasGamemodes ? gamemodes.ResolveObjectives(request) : null;
            return RoundOutcomeBuilder.Build(request, _roundId, _serverSettings.Mode, ModeDisplayName(mode),
                _stationName, RoundSeconds, CollectRoster(), "Operatives", objectives);
        }

        /// <summary>The gamemode asset name, as players know the mode.</summary>
        private static string ModeDisplayName(string gamemodeName) =>
            gamemodeName switch
            {
                null or "" or "unknown" => "",
                "Extended" => "Extended",
                _ when gamemodeName.IndexOf("nuke", StringComparison.OrdinalIgnoreCase) >= 0 => "Nuclear Emergency",
                _ => gamemodeName,
            };

        /// <summary>Everyone who took a body, as their current body stands: a ghost counts as dead.</summary>
        [Server]
        private List<RoundOutcomePlayer> CollectRoster()
        {
            List<RoundOutcomePlayer> roster = new();
            if (!SubSystems.TryGet(out EntitySubSystem entities))
                return roster;

            SubSystems.TryGet(out RoleSubSystem roles);
            foreach (Entity entity in entities.SpawnedPlayers)
            {
                Player player = entity != null && entity.Mind != null ? entity.Mind.player : null;
                if (player == null)
                    continue;

                roster.Add(new RoundOutcomePlayer
                {
                    Ckey = player.Ckey,
                    Name = entity.Mind.DisplayName,
                    Job = roles != null && roles.TryGetRole(player, out RoleData role) ? role.Name : "",
                    Alive = entity.GetComponent<HumanoidGhostController>() == null,
                    Antagonist = roles != null && roles.IsOperative(player),
                });
            }

            return roster;
        }

        [Server]
        private void PublishOutcome(RoundOutcome outcome) => RpcRoundOutcome(outcome);

        /// <summary>Buffered, so a client that connects later (in the beat or the lobby) still gets the last outcome.</summary>
        [ObserversRpc(BufferLast = true)]
        private void RpcRoundOutcome(RoundOutcome outcome)
        {
            LastOutcome = outcome;
            OutcomeReceived?.Invoke(outcome);
        }

        [Server]
        private async UniTask RunEndBeat(CancellationToken cancellationToken)
        {
            _endBeat.Begin(_endBeatSeconds);
            SyncEndBeat();
            try
            {
                TimeSpan second = TimeSpan.FromSeconds(1);
                while (true)
                {
                    await UniTask.Delay(second, ignoreTimeScale: true, cancellationToken: cancellationToken);
                    if (_endBeat.Tick())
                        break;

                    SyncEndBeat();
                }
            }
            finally
            {
                _endBeat.Reset();
                SyncEndBeat();
            }
        }

        [Server]
        private void SyncEndBeat()
        {
            _endSecondsLeft = _endBeat.SecondsLeft;
            _endHeld = _endBeat.Held;
        }

        /// <summary>Admin hold/resume of the summary beat; the same wedge as holding the countdown.</summary>
        [Server]
        private void SetEndHeld(bool held, string ckey)
        {
            if (!_endBeat.SetHeld(held))
                return;

            SyncEndBeat();
            string notice = held
                ? $"{ckey} is holding the round-end summary."
                : $"{ckey} resumed the round-end summary.";
            Log.Information(this, notice, Logs.ServerOnly);
            if (SubSystems.TryGet(out CommsSubSystem comms))
                comms.PostOocSystem(OocChannel.Admin, notice);
        }
    }
}
