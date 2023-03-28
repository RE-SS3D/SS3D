using Coimbra;
using JetBrains.Annotations;
using SS3D.Data;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Core.Settings
{
	[ProjectSettings("SS3D", "Network Settings")]
	public sealed class NetworkSettings : ScriptableSettings
	{
		private const string NetworkSettingsFileName = "network.json";

		private static readonly string NetworkSettingsPath = Paths.GetPath(GamePaths.Config, true) + NetworkSettingsFileName;

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

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();

			SaveNetworkSettingsJsonFile();
		}
  #endif

		/// <summary>
		/// Resets the configurations to what a Client should initially be like, then we load it from the JSON file, followed by the overrides from the command line args.
		/// </summary>
		public static void ResetOnBuiltApplication()
		{
			NetworkSettings networkSettings = GetOrFind<NetworkSettings>();

			Punpun.Information(nameof(NetworkSettings), $"Network settings reset on the built executable");

			networkSettings.NetworkType = NetworkType.Client;
			networkSettings.ServerAddress = string.Empty;
			networkSettings.Ckey = string.Empty;
			networkSettings.ServerPort = ushort.MinValue;
		}

		/// <summary>
		/// Saves the network settings into a file.
		/// </summary>
		private void SaveNetworkSettingsJsonFile()
		{
			string json = JsonUtility.ToJson(this, true);

			Paths.WriteOrCreateIfNotExistsFile(NetworkSettingsPath, json);

			Punpun.Information(this, $"Saved network settings under {NetworkSettingsPath}");
		}

		/// <summary>
		/// Loads the JSON data from the file path.
		/// </summary>
		/// <returns></returns>
		public static void LoadFromJson()
		{
			string jsonFile = ReadOrCreateNetworkSettingsJsonFile();

			NetworkSettings currentSettings = GetOrFind<NetworkSettings>();
			NetworkSettings jsonSettings = JsonUtility.FromJson<NetworkSettings>(jsonFile);

			NetworkSettings networkSettings = string.IsNullOrEmpty(jsonFile) ? currentSettings : jsonSettings;

			Punpun.Information(nameof(NetworkSettings), $"Loading network settings from JSON file");

			Set(networkSettings);
		}

		[NotNull]
		private static string ReadOrCreateNetworkSettingsJsonFile()
		{
			return Paths.ReadOrCreateIfNotExistsFile(NetworkSettingsPath);
		}
	}
}