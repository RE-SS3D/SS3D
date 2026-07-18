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

        private RoundState _currentRoundState = RoundState.Stopped;
        private bool _clientConnected;
        private bool _serverStarted;
        private bool _scriptStarted;

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

            // Disconnect / scene unload re-fires ApplicationInitializing. Re-running the script
            // (and NetworkSession re-join) is what produced post-ScriptComplete Errors and RoleSubSystem
            // duplicate-key exceptions in the multiplayer harness.
            if (_scriptStarted)
            {
                return;
            }

            _scriptStarted = true;

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

        private bool IsRoundState(RoundState target)
        {
            if (_currentRoundState == target)
            {
                return true;
            }

            // SyncVar can advance before/without our event listener seeing every transition
            // (e.g. after scene churn). Prefer live RoundSubSystem state when available.
            if (SubSystems.TryGet(out RoundSubSystem roundSystem) && roundSystem.CurrentRoundState == target)
            {
                _currentRoundState = target;
                return true;
            }

            return false;
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

                    // Drive nested enumerators ourselves (e.g. WaitUntil). Yielding them to Unity
                    // lets TimeoutException escape through SetupCoroutine instead of our catch.
                    if (step.Current is IEnumerator nested)
                    {
                        while (true)
                        {
                            bool nestedMoved;

                            try
                            {
                                nestedMoved = nested.MoveNext();
                            }
                            catch (Exception ex)
                            {
                                failureReason = $"{instruction.Opcode}:{ex.Message}";
                                break;
                            }

                            if (!nestedMoved)
                            {
                                break;
                            }

                            yield return nested.Current;
                        }

                        if (failureReason != null)
                        {
                            break;
                        }
                    }
                    else
                    {
                        yield return step.Current;
                    }
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

                    // FishNet marks the client Connected before Game loads additively and before
                    // UnauthorizedPlayer auth finishes. Lobby actions need a Player with a ckey
                    // owned by this connection — otherwise start_round hits PermissionSubSystem
                    // with a null ckey and the round never starts.
                    if (!IsServerRole())
                    {
                        yield return WaitUntil(IsLobbyReady, DefaultWaitTimeoutSeconds, "wait_lobby");
                    }

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
                    yield return WaitUntil(() => IsRoundState(target), DefaultWaitTimeoutSeconds, "wait_round");
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

        private static bool IsLobbyReady()
        {
            if (!SubSystems.TryGet(out PlayerSubSystem playerSystem))
            {
                return false;
            }

            string ckey = playerSystem.GetCkey(InstanceFinder.ClientManager.Connection);
            return !string.IsNullOrEmpty(ckey);
        }

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
            // One frame so Serilog can flush ScriptComplete/ScriptFailed, then hard-exit.
            // Application.Quit() in player builds still tears scenes down and re-enters
            // ApplicationInitializing (NetworkSession re-joins, duplicate automation) before the
            // process dies — that noise fails the harness after a successful scenario.
            yield return null;
            System.Environment.Exit(0);
        }
    }
}
