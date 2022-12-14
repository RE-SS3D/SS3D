using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Managing;
using SS3D.Core.Behaviours;
using SS3D.Core.Events;
using SS3D.Core.Utils;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Helps the NetworkManager to understand what we should do in this instance,
    /// if we are a server, or a client, and process respective data.
    ///
    /// TODO: Could use a refactor
    /// </summary>
    public sealed class SessionNetworkHelper : Actor
    {
        private static List<string> _commandLineArgs;

        private static bool _isHost;
        private static bool _serverOnly;
        private static string _ip;
        private static string _ckey;
        
        private const string EditorServerIP = "127.0.0.1";
        private const string EditorServerUsername = "editorUser";

        private void Awake()
        {
            ProcessCommandLineArgs();
        }

        // Gets the command line arguments from the executable, for example: "-server=localhost"
        private static void GetCommandLineArgs()
        {
            try
            {
                _commandLineArgs = Environment.GetCommandLineArgs().ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(SessionNetworkHelper)}] - GetCommandLineArgs: {e}");
                throw;
            }
        }

        // Uses the args to determine if we have to connect or host, etc
        private static void ProcessCommandLineArgs()
        {
            if (Application.isEditor)
            {
                _isHost = ApplicationStateManager.IsServer && ApplicationStateManager.IsClient;
                _ip = EditorServerIP;
                _ckey = EditorServerUsername;
                _serverOnly = ApplicationStateManager.IsServer && !ApplicationStateManager.IsClient;
                Debug.Log($"[{nameof(SessionNetworkHelper)}] - Testing application on the editor as {_ckey}");
            }
            else
            {
                GetCommandLineArgs();
                foreach (string arg in _commandLineArgs)
                {
                    if (arg.Contains(CommandLineArgs.Host))
                    {
                        _isHost = true;
                        Debug.Log($"[{nameof(SessionNetworkHelper)}] - Command args - {CommandLineArgs.Host} - is true");
                    }
                    
                    if (arg.Contains(CommandLineArgs.Ip))
                    {
                        _ip = arg.Replace(CommandLineArgs.Ip, "");
                        Debug.Log($"[{nameof(SessionNetworkHelper)}] - Command args - {CommandLineArgs.Ip} - {_ip}");
                    }

                    if (arg.Contains(CommandLineArgs.Ckey))
                    {
                        _ckey = arg.Replace(CommandLineArgs.Ckey, "");
                        ApplicationStateManager.SetServerOnly(false);
                        Debug.Log($"[{nameof(SessionNetworkHelper)}] - Command args - {CommandLineArgs.Ckey} - {_ckey}");                        
                    }

                    if (arg.Contains(CommandLineArgs.SkipIntro))
                    {
                        ApplicationStateManager.SetSkipIntro(true);
                        Debug.Log($"[{nameof(SessionNetworkHelper)}] - Command args - {CommandLineArgs.SkipIntro} - {true}");
                    }

                    if (arg.Contains(CommandLineArgs.EnableDiscordIntegration))
                    {
                        ApplicationStateManager.SetDiscordIntegration(true);
                        Debug.Log($"[{nameof(SessionNetworkHelper)}] - Command args - {CommandLineArgs.EnableDiscordIntegration} - {true}");
                    }
                    
                    if (arg.Contains(CommandLineArgs.ServerOnly))
                    {
                        ApplicationStateManager.SetServerOnly(true);
                        Debug.Log($"[{nameof(SessionNetworkHelper)}] - Command args - {CommandLineArgs.ServerOnly} - {true}");
                    }
                }

                Debug.Log($"[{nameof(SessionNetworkHelper)}] - Testing application on executable");
            }

            LocalPlayerAccountUtility.UpdateCkey(_ckey);
        }

        /// <summary>
        /// Uses the processed args to proceed with game network initialization
        /// </summary>
        public static void InitiateNetworkSession()
        {
            ProcessCommandLineArgs();

            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (networkManager == null)
            {
                networkManager = InstanceFinder.NetworkManager;
                InitiateNetworkSession();
            }

            ApplicationMode applicationMode;

            if (_serverOnly)
            {
                Debug.Log($"[{nameof(SessionNetworkHelper)}] - Hosting a new headless server");
                networkManager.ServerManager.StartConnection();
                applicationMode = ApplicationMode.ServerOnly;
            }
            else if (_isHost)
            {
                Debug.Log($"[{nameof(SessionNetworkHelper)}] - Hosting a new server");
                networkManager.ServerManager.StartConnection();
                networkManager.ClientManager.StartConnection();

                applicationMode = ApplicationMode.Host;
            }
            else
            {
                Debug.Log($"[{nameof(SessionNetworkHelper)}] - Joining server {_ip} as {_ckey}");
                networkManager.ClientManager.StartConnection(_ip);

                applicationMode = ApplicationMode.Client;
            }

            ApplicationStartedEvent applicationStartedEvent = new(_ckey, applicationMode);
            applicationStartedEvent.Invoke(null!);
        }
    }
}