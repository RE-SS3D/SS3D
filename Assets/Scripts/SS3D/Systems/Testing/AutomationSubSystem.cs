using Coimbra;
using Coimbra.Services.Events;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using SS3D.Application;
using SS3D.Application.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using SS3D.Networking.Settings;
using SS3D.Systems.Entities;
using SS3D.Systems.IngameConsoleSystem;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;

namespace SS3D.Systems.Testing
{
    /// <summary>
    /// Drives a headless client/host/dedicated-server process through a scripted sequence of real
    /// gameplay actions for the multiplayer test harness (Testing/multiplayer/), using the same
    /// client-&gt;server broadcast/RPC APIs the UI calls
    /// (SS3D.Systems.Lobby.UI.LobbyReadyView, SS3D.Systems.Lobby.UI.ChangeRoundStateView) - never
    /// simulated input, never the Editor-only stub broadcasts on
    /// <see cref="ReadyPlayersSubSystem"/>/<see cref="RoundSubSystem"/>, since those don't compile
    /// into a real built player/server.
    /// <para>
    /// Self-bootstraps like <see cref="SS3D.Systems.ScreenEffects.ScreenEffectsSubSystem"/> instead
    /// of living in Boot.unity - see AGENTS.md "Composition, prefabs, and UI". A no-op unless
    /// <see cref="ApplicationSettings.TestScriptPath"/> is set (the "-testscript=" CLI arg), so this
    /// has zero effect on normal play.
    /// </para>
    /// </summary>
    public sealed class AutomationSubSystem : SubSystem
    {
        private const float DefaultWaitTimeoutSeconds = 30f;
        private const float QuitDelaySeconds = 1f;

        private RoundState _currentRoundState = RoundState.Stopped;
        private bool _clientConnected;
        private bool _serverStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SubSystems.TryGet(out AutomationSubSystem _))
            {
                return;
            }

            GameObject host = new(nameof(AutomationSubSystem));
            DontDestroyOnLoad(host);
            host.AddComponent<AutomationSubSystem>();
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            ApplicationInitializing.AddListener(HandleApplicationInitializing);
        }

        private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            if (string.IsNullOrEmpty(applicationSettings.TestScriptPath))
            {
                return;
            }

            SubscribeToConnectionEvents();
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));

            StartCoroutine(RunScript(applicationSettings.TestScriptPath));
        }

        private void SubscribeToConnectionEvents()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;

            networkManager.ClientManager.OnClientConnectionState += HandleClientConnectionState;
            networkManager.ServerManager.OnServerConnectionState += HandleServerConnectionState;
        }

        private void HandleClientConnectionState(ClientConnectionStateArgs args)
        {
            _clientConnected = args.ConnectionState == LocalConnectionState.Started;
        }

        private void HandleServerConnectionState(ServerConnectionStateArgs args)
        {
            _serverStarted = args.ConnectionState == LocalConnectionState.Started;
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            _currentRoundState = e.RoundState;
        }

        private IEnumerator RunScript(string scriptPath)
        {
            IReadOnlyList<AutomationInstruction> instructions = null;
            string failureReason = null;

            try
            {
                instructions = AutomationScript.Load(scriptPath);
            }
            catch (Exception ex)
            {
                failureReason = $"CouldNotLoadScript:{ex.Message}";
            }

            if (failureReason != null)
            {
                TestSignal.Emit(this, "ScriptFailed", failureReason);
                yield return QuitAfterDelay();
                yield break;
            }

            foreach (AutomationInstruction instruction in instructions)
            {
                IEnumerator step = RunInstruction(instruction);

                while (true)
                {
                    bool moved;

                    try
                    {
                        moved = step.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        failureReason = $"{instruction.Opcode}:{ex.Message}";
                        break;
                    }

                    if (!moved)
                    {
                        break;
                    }

                    yield return step.Current;
                }

                if (failureReason != null)
                {
                    TestSignal.Emit(this, "ScriptFailed", failureReason);
                    yield return QuitAfterDelay();
                    yield break;
                }
            }

            TestSignal.Emit(this, "ScriptComplete");
            yield return QuitAfterDelay();
        }

        private IEnumerator RunInstruction(AutomationInstruction instruction)
        {
            switch (instruction.Opcode)
            {
                case "wait_connected":
                    yield return WaitUntil(IsConnected, DefaultWaitTimeoutSeconds, "wait_connected");
                    TestSignal.Emit(this, IsServerRole() ? "ServerReady" : "ClientConnected");
                    break;

                case "ready":
                    SendReady(true);
                    break;

                case "start_round":
                    InstanceFinder.ClientManager.Broadcast(new ChangeRoundStateMessage(true));
                    break;

                case "wait_round":
                    RoundState target = Enum.Parse<RoundState>(instruction.Args[0], ignoreCase: true);
                    yield return WaitUntil(() => _currentRoundState == target, DefaultWaitTimeoutSeconds, "wait_round");
                    TestSignal.Emit(this, "RoundStateChanged", target.ToString());
                    break;

                case "embark":
                    Embark();
                    TestSignal.Emit(this, "PlayerEmbarked");
                    break;

                case "console":
                    RunConsoleCommand(instruction.ArgsJoined);
                    break;

                case "wait_seconds":
                    yield return new WaitForSeconds(float.Parse(instruction.Args[0]));
                    break;

                case "disconnect":
                    Disconnect();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown automation instruction '{instruction.Opcode}'");
            }
        }

        private bool IsServerRole()
        {
            NetworkSettings networkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();

            return networkSettings.NetworkType == SS3D.Networking.NetworkType.DedicatedServer;
        }

        private bool IsConnected() => IsServerRole() ? _serverStarted : _clientConnected;

        private void SendReady(bool ready)
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            string ckey = playerSystem.GetCkey(InstanceFinder.ClientManager.Connection);

            InstanceFinder.ClientManager.Broadcast(new ChangePlayerReadyMessage(ckey, ready));
        }

        private void Embark()
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();

            Player player = playerSystem.GetPlayer(InstanceFinder.ClientManager.Connection);
            entitySystem.CmdSpawnLatePlayer(player);
        }

        private void RunConsoleCommand(string commandLine)
        {
            CommandsController commandsController = FindFirstObjectByType<CommandsController>();

            if (commandsController == null)
            {
                throw new InvalidOperationException("No CommandsController found in the scene to run a console instruction against.");
            }

            commandsController.ClientProcessCommand(commandLine);
        }

        private void Disconnect()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;

            if (IsServerRole())
            {
                networkManager.ServerManager.StopConnection(true);
            }
            else
            {
                networkManager.ClientManager.StopConnection();
            }
        }

        private IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string label)
        {
            float startTime = Time.time;

            while (!condition())
            {
                if (Time.time - startTime > timeoutSeconds)
                {
                    throw new TimeoutException($"Timed out after {timeoutSeconds}s waiting for {label}");
                }

                yield return null;
            }
        }

        private IEnumerator QuitAfterDelay()
        {
            yield return new WaitForSeconds(QuitDelaySeconds);

            UnityEngine.Application.Quit();
        }
    }
}
