using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using SS3D.Core.Settings;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Core.Utils
{
    /// <summary>
    /// Loads the command line args and processes them for the application settings.
    /// </summary>
    public class CommandLineArgsSystem : Behaviours.System
    {
        private List<string> _commandLineArgs;
        private ApplicationSettings _applicationSettings;

        /// <summary>
        /// Loads the application settings data, doing appropriate changes depending on play mode.
        /// </summary>
        public void LoadApplicationSettings()
        {
            _applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            if (!Application.isEditor)
            {
                _applicationSettings.ResetOnBuildApplication();
            }
        }

        /// <summary>
        /// Loads and processes all command line args
        /// </summary>
        public void ProcessCommandLineArgs()
        {
            LoadCommandLineArgs();

            _applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            foreach (string arg in _commandLineArgs)
            {
                ProcessCommandArg(arg);
            }
        }

        /// <summary>
        /// Gets the command line arguments from the executable, for example: "-server=localhost"
        /// </summary>
        private void ProcessCommandArg(string arg)
        {
            if (arg.Contains(CommandLineArgs.Host))
            {
                _applicationSettings.NetworkType = NetworkType.Host;
            }

            if (arg.Contains(CommandLineArgs.Ip))
            {
                _applicationSettings.ServerAddress = arg.Replace(CommandLineArgs.Ip, "");
            }

            if (arg.Contains(CommandLineArgs.Ckey))
            {
                string ckey = arg.Replace(CommandLineArgs.Ckey, "");

                _applicationSettings.Ckey = ckey;
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
                _applicationSettings.NetworkType = NetworkType.ServerOnly;
            }

            LocalPlayer.UpdateCkey(_applicationSettings.Ckey);

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
                Punpun.Say(this, $"Failed to load command line arguments: {e}");
                throw;
            }
        }
    }
}