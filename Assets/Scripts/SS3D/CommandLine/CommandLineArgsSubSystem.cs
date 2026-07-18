using Coimbra;
using Coimbra.Services.Events;
using SS3D.Logging;
using SS3D.Networking;
using SS3D.Networking.Settings;
using SS3D.Utils;
using SS3D.Application;
using SS3D.Application.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Core.Behaviours;

namespace SS3D.CommandLine
{
    /// <summary>
    /// Loads the command line args and processes them for the application settings.
    /// </summary>
    public sealed class CommandLineArgsSubSystem : SubSystem
    {
        private List<string> _commandLineArgs;

        private NetworkSettings _networkSettings;
        private ApplicationSettings _applicationSettings;

        protected override void OnAwake()
        {
            base.OnAwake();

            ApplicationPreInitializing.AddListener(HandleApplicationPreInitializing);
        }

        private void HandleApplicationPreInitializing(ref EventContext context, in ApplicationPreInitializing e)
        {
            ProcessCommandLineArgs();

            // Log path depends on NetworkType/Ckey from the CLI above — must run here, not from
            // another PreInitializing listener that may fire before us (see NetworkSessionSubSystem.GetFileLogName).
            LogManager.Initialize(NetworkSessionSubSystem.GetFileLogName(_networkSettings));
            Log.Debug(this, "Getting command line args", Logs.Important);
        }

        /// <summary>
        /// Loads and processes all command line args
        /// </summary>
        private void ProcessCommandLineArgs()
        {
            LoadCommandLineArgs();

            if (!UnityEngine.Application.isEditor)
            {
                NetworkSettings.ResetOnBuiltApplication();
                ApplicationSettings.ResetOnBuiltApplication();
            }
            
            _networkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();
            _applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            foreach (string arg in _commandLineArgs)
            {
                ProcessCommandArg(arg);
            }
        }

        public bool HasCommandLineArgs()
        {
            LoadCommandLineArgs();

            return !_commandLineArgs.OneElementOnly();
        }

        /// <summary>
        /// Gets the command line arguments from the executable, for example: "-server=localhost"
        /// </summary>
        private void ProcessCommandArg(string arg)
        {
            if (arg.Contains(CommandLineArgs.Host))
            {
                _networkSettings.NetworkType = NetworkType.Host;
            }

            if (arg.Contains(CommandLineArgs.Ip))
            {
                _networkSettings.NetworkType = NetworkType.Client;
                _networkSettings.ServerAddress = arg.Replace(CommandLineArgs.Ip, "");
            }

            if (arg.Contains(CommandLineArgs.Ckey))
            {
                string ckey = arg.Replace(CommandLineArgs.Ckey, "");

                _networkSettings.Ckey = ckey;
            }

            if (arg.Contains(CommandLineArgs.Port))
            {
                string port = arg.Replace(CommandLineArgs.Port, "");

                _networkSettings.ServerPort = Convert.ToUInt16(port);
            }

            if (arg.Contains(CommandLineArgs.SkipIntro))
            {
                _applicationSettings.SkipIntro = true;
            }

            if (arg.Contains(CommandLineArgs.EnableDiscordIntegration))
            {
                _applicationSettings.EnableDiscord = true;
            }

            if (arg.Contains(CommandLineArgs.ServerOnly))
            {
                _networkSettings.NetworkType = NetworkType.DedicatedServer;
            }

            if (arg.Contains(CommandLineArgs.ForceLauncher))
            {
                _applicationSettings.ForceLauncher = true;
            }

            if (arg.Contains(CommandLineArgs.TestScript))
            {
                _applicationSettings.TestScriptPath = arg.Replace(CommandLineArgs.TestScript, "");
            }
        }

        /// <summary>
        /// Tries to load the command line args from the executable.
        /// </summary>
        private void LoadCommandLineArgs()
        {
            try
            {
                _commandLineArgs = Environment.GetCommandLineArgs().ToList();
            }
            catch (Exception e)
            {
                Log.Error(this, e, "Failed to load command line arguments");
                throw;
            }
        }
    }
}