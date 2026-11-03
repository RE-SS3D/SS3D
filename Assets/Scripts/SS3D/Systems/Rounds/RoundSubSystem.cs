using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.WorldReadiness;
using SS3D.Logging;
using SS3D.Systems.Comms;
using SS3D.Systems.Persistence;
using SS3D.Systems.Tile.Validation;
using SS3D.Systems.Rounds.Messages;
using SS3D.Systems.WorldReadiness;
using System;
using System.Linq;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Round system base implementation for basic round functionality
    /// </summary>
    public sealed partial class RoundSubSystem : RoundSubSystemBase
    {
        /// <summary>
        /// Round loop runner
        /// </summary>
        [Server]
        protected override async UniTask ProcessChangeRoundState(ChangeRoundStateMessage m, CancellationToken cancellationToken)
        {
            if (!IsServer)
            {
                return;
            }

            if (m.State)
            {
                await StartRoundSequence(cancellationToken);
            }
            else
            {
                await StopRoundSequence(cancellationToken);
            }
        }

        [Server]
        private async UniTask StartRoundSequence(CancellationToken cancellationToken)
        {
            _serverSettingsError = "";
            PersistenceSubSystem persistence = SubSystems.Get<PersistenceSubSystem>();
            if (string.IsNullOrEmpty(_serverSettings.Map))
                _serverSettings.Map = DefaultStation(persistence.ListStationTemplates().ToArray());
            if (string.IsNullOrEmpty(_serverSettings.Map))
            {
                _serverSettingsError = "No saved maps available. Import or save a station before starting a round.";
                _stationLoadState = StationLoadState.Failed;
                SetAutoStartHeld(true, "no saved maps");
                return;
            }

            RoundState = RoundState.Preparing;
            RoundSeconds = 0;
            BeginRoundId();
            _roundFellBackToExtended = false;
            _pendingOutcome = null;
            _stationName = _serverSettings.Map;
            _stationLoadState = StationLoadState.Loading;
            try
            {
                // Let the loading state reach the UI before disk parsing / object restoration.
                await UniTask.NextFrame(cancellationToken: cancellationToken);
                Tile.TileSubSystem tiles = null;
                await UniTask.WaitUntil(() => SubSystems.TryGet(out tiles) && tiles.CurrentMap != null
                    && tiles.Loader != null && tiles.Loader.IsInitialized, cancellationToken: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                // Every start restores the selected template, including after a cancelled countdown.
                // A previous round's in-memory station is never an implicit selection.
                bool loaded = await persistence.LoadStationTemplateAsync(_serverSettings.Map);
                cancellationToken.ThrowIfCancellationRequested();
                if (!loaded)
                {
                    FailStationLoad("Station could not be loaded. Select another saved map.");
                    return;
                }
                MapValidationReport validation = MapValidationService.Validate(tiles, _serverSettings.Mode);
                if (!validation.CanStart)
                {
                    FailStationLoad(validation.Describe(_serverSettings.Map, _serverSettings.Mode, saved: false));
                    return;
                }
                tiles.SyncFloorDecalsToClients();
                _stationLoadState = StationLoadState.Initializing;
                await PrepareRound(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                _stationLoadState = StationLoadState.Ready;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                FailStationLoad("Station load failed. Select a map and try again.");
                Log.Error(this, "Station restore failed: {error}", Logs.ServerOnly, exception.Message);
                return;
            }

            await ProcessRoundTick(cancellationToken);
        }

        [Server]
        private void FailStationLoad(string message)
        {
            // Also log it: a dedicated server has no lobby UI to show the reason, and the round just drops to Stopped.
            Log.Warning(this, "Round start aborted: {reason}", Logs.ServerOnly, message);
            _serverSettingsError = message;
            _stationLoadState = StationLoadState.Failed;
            // Retrying the same broken map every countdown would only spam the log.
            SetAutoStartHeld(true, "the station failed to load");
            RoundState = RoundState.Stopped;
            RoundSeconds = 0;
        }

        [Server]
        private async UniTask StopRoundSequence(CancellationToken cancellationToken)
        {
            if (RoundState == RoundState.Stopped)
            {
                return;
            }

            if (RoundState != RoundState.Ending)
            {
                await ProcessEndRound(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            await StopRound(cancellationToken);
        }

        /// <summary>
        /// Prepares the round before starting — waits for world readiness, not a fixed delay.
        /// </summary>
        [Server]
        protected override async UniTask PrepareRound(CancellationToken cancellationToken)
        {
            Log.Information(this, "Preparing round", Logs.ServerOnly);

            RoundState = RoundState.Preparing;

            WorldReadinessSubSystem readiness = null;
            await UniTask.WaitUntil(
                () => SubSystems.TryGet(out readiness),
                cancellationToken: cancellationToken);

            await readiness.WaitUntilAsync(WorldReadyPhase.WorldReady, cancellationToken);
        }

        /// <summary>
        /// Process the round tick until the round ends
        /// </summary>
        [Server]
        protected override async UniTask ProcessRoundTick(CancellationToken cancellationToken)
        {
            Log.Information(this, "Starting {seconds} seconds warmup tick", Logs.ServerOnly, _warmupSeconds);

            RoundSeconds = _warmupSeconds;
            _forceStart = false;
            CancelTick();
            TickCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            System.TimeSpan second = System.TimeSpan.FromSeconds(1);

            RoundState = RoundState.WarmingUp;

            while (IsWarmingUp && RoundSeconds > 0 && !_forceStart)
            {
                await UniTask.Delay(second, cancellationToken: TickCancellationToken.Token);
                RoundSeconds--;
            }

            cancellationToken.ThrowIfCancellationRequested();

            RoundSeconds = 0;
            RoundState = RoundState.Ongoing;
            Log.Information(this, "Round {roundId} started", Logs.ServerOnly, _roundId);
            SubSystems.Get<CommsSubSystem>()?.SendAnnouncement(
                "Welcome to the station crew, enjoy your stay.",
                CommsAudioTrackIds.StationWelcome);
            if (SubSystems.TryGet(out CommsSubSystem comms)) comms.PostOocSystem(OocChannel.Ooc, "The round has started.");

            while (IsOngoing)
            {
                await UniTask.Delay(second, cancellationToken: TickCancellationToken.Token);
                RoundSeconds++;
                int secondsLeft = _serverSettings.RoundLengthMinutes * 60 - RoundSeconds;
                if (secondsLeft <= 0)
                {
                    RequestRoundEnd(RoundEndRequest.Timer);
                    return;
                }

                PostRoundEndWarning(secondsLeft);
            }
        }

        /// <summary>
        /// Ending: fixes the outcome (if no trigger did yet), shows the summary to everyone and runs the
        /// 30 s beat, which an admin can hold. The world keeps running; <see cref="StopRound"/> follows.
        /// </summary>
        [Server]
        protected override async UniTask ProcessEndRound(CancellationToken cancellationToken)
        {
            // A stop that bypassed RequestRoundEnd (test stubs) still gets a record, as an admin end.
            RoundOutcome outcome = _pendingOutcome ?? BuildOutcome(RoundEndRequest.Admin);
            _pendingOutcome = null;
            RoundState = RoundState.Ending;
            CancelTick();
            Log.Information(this, "Round {roundId} ended after {seconds} s: {trigger}, winner {winner}", Logs.ServerOnly,
                _roundId, RoundSeconds, outcome.Trigger, outcome.Winner);
            if (SubSystems.TryGet(out CommsSubSystem comms))
                comms.PostOocSystem(OocChannel.Ooc, $"The round has ended. {RoundOutcomeText.Title(outcome.Winner)}: {outcome.Reason}");

            AppendRoundHistory(outcome);
            PublishOutcome(outcome);
            await RunEndBeat(cancellationToken);
        }

        [Server]
        private void AppendRoundHistory(RoundOutcome outcome)
        {
            if (!SubSystems.TryGet(out PersistenceSubSystem persistenceSubSystem))
            {
                return;
            }

            string mapId = SubSystems.TryGet(out Tile.TileSubSystem tileSubSystem) && tileSubSystem.CurrentMap != null
                ? tileSubSystem.CurrentMap.gameObject.name
                : string.Empty;

            persistenceSubSystem.AppendRoundHistory(new RoundHistoryEntry
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                gamemode = outcome.Mode,
                mapId = mapId,
                playerCount = ServerManager?.Clients.Count ?? 0,
                durationSeconds = outcome.DurationSeconds,
                fallbackMap = _roundFellBackToExtended,
                trigger = outcome.Trigger.ToString(),
                winner = outcome.Winner.ToString(),
                operativeCkeys = outcome.Players.Where(p => p.Antagonist).Select(p => p.Ckey).ToArray(),
            });
        }

        [Server]
        protected override async UniTask StopRound(CancellationToken cancellationToken)
        {
            if (RoundState == RoundState.Stopped)
            {
                return;
            }

            RoundState = RoundState.Stopped;
            RoundSeconds = 0;
            UnloadStation();

            await UniTask.Delay(System.TimeSpan.FromMilliseconds(500), cancellationToken: cancellationToken);
            AppendResetHealth();
            ExitIfRoundLimitReached();
        }

        /// <summary>
        /// Wipes the finished round's station once its crew is despawned (Stopped), so the lobby sits
        /// over an empty world instead of a frozen view of the old station. The next Start restores
        /// the selected template from disk anyway, so nothing a new round needs is lost.
        /// </summary>
        [Server]
        private void UnloadStation()
        {
            HandleUnsavedMapEditsBeforeUnload();
            if (SubSystems.TryGet(out Tile.TileSubSystem tiles) && tiles.CurrentMap != null)
            {
                tiles.CurrentMap.Clear();
                tiles.SyncFloorDecalsToClients();
            }

            _stationLoadState = StationLoadState.NotLoaded;
        }

#if UNITY_EDITOR
        /// <summary>
        /// This method facilitates automated testing, and is not to be used in production.
        /// It simulates a ChangeRoundStateMessage broadcast received from a client, and
        /// is handled normally by the server. Method required because the server cannot
        /// broadcast to itself. Note that authentication in RoundSystemBase has been
        /// bypassed by this method.
        /// </summary>
        /// <param name="m">The ChangeRoundStateMessage apparently broadcast</param>
        [Server]
        public void ChangeRoundStateMessageStubBroadcast(ChangeRoundStateMessage m)
        {
            RequestRoundStateChange(m);
        }

        /// <summary>
        /// Automation only: replaces the server settings and rules the way <c>-sandbox</c> does at boot,
        /// so a pilot in the editor can run the sandbox profile without a command-line flag.
        /// </summary>
        [Server]
        public void ApplyServerSettingsForTesting(ServerSettings settings)
        {
            _serverSettings = settings;
            _allowLateJoin = settings.AllowLateJoin;
            _publicMode = settings.Mode;
            _motd = settings.Motd;
            ApplyServerRules();
        }

        /// <summary>
        /// Automation only: the round-end summary beat lasts <paramref name="seconds"/> instead of
        /// <see cref="RoundEndBeat.Seconds"/>, so tests that stop rounds for set-up don't pay 30 s per stop.
        /// Tests about the round end itself put it back. Lasts for this server session.
        /// </summary>
        [Server]
        public void SetEndBeatSecondsForTesting(int seconds) => _endBeatSeconds = Math.Max(1, seconds);
#endif
    }
}
