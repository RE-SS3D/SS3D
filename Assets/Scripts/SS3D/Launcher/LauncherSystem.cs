using Coimbra;
using SS3D.Core.Settings;
using SS3D.Data.Enums;
using SS3D.SceneManagement;
using System;
using UnityEngine;

namespace SS3D.Launcher
{
	/// <summary>
	/// System used to control launcher functions.
	/// </summary>
	public sealed class LauncherSystem : Core.Behaviours.System
	{
		/// <summary>
		/// Launches the game.
		/// </summary>
		/// <param name="networkType">The network mode used on the game.</param>
		/// <param name="ckey">The ckey used on the game.</param>
		/// <param name="ip">The IP address to connect to.</param>
		/// <param name="port">The port number to use.</param>
		public void LaunchGame(NetworkType networkType, string ckey, string ip, string port)
		{
			if (!Application.isEditor)
			{
				NetworkSettings.ResetOnBuiltApplication();
			}

			NetworkSettings networkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();

			networkSettings.NetworkType = networkType;
			networkSettings.Ckey = ckey;
			networkSettings.ServerAddress = ip;
			networkSettings.ServerPort = Convert.ToUInt16(port);

			Scene.LoadAsync(Scenes.Intro.Name);
		}
	}
}