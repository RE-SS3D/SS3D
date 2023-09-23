using Coimbra;
using SS3D.Core.Settings;
using SS3D.Data;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Networking.Settings
{
	[ProjectSettings("SS3D/Core", "Network Settings")]
	public sealed class NetworkSettings : ScriptableSettings
	{
		/// <summary>
		/// Defines what type of connection to start when starting the application.
		/// Defined via command line args when in a built executable or the value in the Project Settings window.
		/// </summary>
		[Tooltip("The selected option is only considered when in Editor. Built executables use the command args.")]
		[Header("Network Settings")]
		public NetworkType NetworkType;

		/// <summary>
		/// The Ckey used to authenticate users. Will be used along an API key when said API exists.
		/// TODO: Update this when we have an API.
		/// Defined via command line args when in a built executable or the EditorServerCkey when in the Editor.
		/// </summary>
		public string Ckey = "unknown";

		/// <summary>
		/// The server address used when we start connecting to a server.
		/// Defined via command line args when in a built executable or the EditorServerAddress when in the Editor.
		/// </summary>
		public string ServerAddress = "127.0.0.1";

		/// <summary>
		/// The server port used when start connecting to a server.
		/// Defined via command line args when in a built executable or the EditorServerAddress when in the Editor.
		/// </summary>
		public ushort ServerPort = 2222;

		private const string NetworkSettingsFileName = "network.json";

		private static readonly string NetworkSettingsPath = Paths.GetPath(GamePaths.Config, true) + NetworkSettingsFileName;

		/// <summary>
		/// Enables Fish-net's bandwidth UI in the game.
		/// </summary>
		[Header("Debug Settings")]
		public bool EnableNetworkBandwidthUsageStats;

		/// <summary>
		/// Resets the configurations to what a Client should initially be like, then we load it from the JSON file, followed by the overrides from the command line args.
		/// </summary>
		public static void ResetOnBuiltApplication()
		{
			NetworkSettings networkSettings = GetOrFind<NetworkSettings>();

			Log.Information(nameof(NetworkSettings), "Network settings reset on the built executable");

			networkSettings.NetworkType = NetworkType.Client;
			networkSettings.ServerAddress = string.Empty;
			networkSettings.Ckey = string.Empty;
			networkSettings.ServerPort = ushort.MinValue;
		}
	}
}