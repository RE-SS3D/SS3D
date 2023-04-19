using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using SS3D.Core.Settings;
using SS3D.Logging;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Core.Utils
{
    /// <summary>
    /// Loads the command line args and processes them for the application settings.
    /// </summary>
    public sealed class CommandLineArgsSystem : Behaviours.System
    {
        private List<string> _commandLineArgs;

        private NetworkSettings _networkSettings;
        private ApplicationSettings _applicationSettings;

        /// <summary>
        /// Loads and processes all command line args
        /// </summary>
        public void ProcessCommandLineArgs()
        {
            LoadCommandLineArgs();

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
                Punpun.Information(this,e,$"Failed to load command line arguments");
                throw;
            }
        }
    }
}