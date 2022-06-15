using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using FishNet;
using FishNet.Managing;
using SS3D.Core.Networking.UI_Helper;
using SS3D.Core.Networking.Utils;
using UnityEngine;
using UriParser = SS3D.Utils.UriParser;

namespace SS3D.Core.Networking.Helper
{
    /// <summary>
    /// Helps the NetworkManager to understand what we should do in this instance,
    /// if we are a server, or a client, and process respective data.
    /// </summary>
    public sealed class SessionNetworkHelper : MonoBehaviour
    {
        private ApplicationStateManager _applicationStateManager;
        private NetworkManager _networkManager;

        private List<string> _commandLineArgs;
        
        private bool _isHost;
        private string _ip;
        private string _ckey;
        
        private void Awake()
        {
            Setup();
            ProcessCommandLineArgs();
        }
        
        private void Setup()
        {
            // Uses the event service to listen to lobby events
            IEventService eventService = ServiceLocator.Shared.Get<IEventService>();
            eventService?.AddListener<ServerConnectionView.RetryButtonClicked>(InitiateNetworkSession);
            
            _applicationStateManager = ApplicationStateManager.Instance;
            _networkManager = InstanceFinder.NetworkManager;
        }

        // Gets the command line arguments from the executable, for example: "-server=localhost"
        private void GetCommandLineArgs()
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
        private void ProcessCommandLineArgs()
        {
            if (Application.isEditor)
            {
                _isHost = !_applicationStateManager.TestingClientInEditor;
                _ip = "localhost";
                _ckey = "editorUser";
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
                        Debug.Log($"[{nameof(SessionNetworkHelper)}] - Command args - {CommandLineArgs.Ckey} - {_ckey}");                        
                    }

                    if (arg.Contains(CommandLineArgs.SkipIntro))
                    {
                        _applicationStateManager.SetSkipIntro(true);
                        Debug.Log($"[{nameof(SessionNetworkHelper)}] - Command args - {CommandLineArgs.SkipIntro} - {true}");
                    }

                    if (arg.Contains(CommandLineArgs.DisableDiscordIntegration))
                    {
                        _applicationStateManager.SetDisableDiscordIntegration(true);
                        Debug.Log($"[{nameof(SessionNetworkHelper)}] - Command args - {CommandLineArgs.DisableDiscordIntegration} - {true}");
                    }
                }

                Debug.Log($"[{nameof(SessionNetworkHelper)}] - Testing application on executable");
            }

            LocalPlayerAccountManager.UpdateCkey(_ckey);
        }

        /// <summary>
        /// Uses the processed args to proceed with game network initialization
        /// </summary>
        public void InitiateNetworkSession()
        {
            if (_networkManager == null)
            {
                _networkManager = InstanceFinder.NetworkManager;
            }

            if (_isHost)
            {
                Debug.Log($"[{nameof(SessionNetworkHelper)}] - Hosting a new server");
                _networkManager.ServerManager.StartConnection();
            }

            else
            {
                Debug.Log($"[{nameof(SessionNetworkHelper)}] - Joining to server {_ip} as {_ckey}");
                _networkManager.ClientManager.StartConnection(UriParser.TryParseIpAddress(_ip).ToString());
            }
        }
        
        // Overload to match the event type
        private void InitiateNetworkSession(object sender, ServerConnectionView.RetryButtonClicked e)
        {
            InitiateNetworkSession();
        }
    }
}