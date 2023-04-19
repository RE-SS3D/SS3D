using Coimbra;
using SS3D.Core.Settings;
using SS3D.Data.Enums;
using SS3D.SceneManagement;
using SS3D.Utils;
using System;
using UnityEngine;

namespace SS3D.Launcher
{
	public sealed class LauncherSystem : Core.Behaviours.System
	{
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

			Scene.LoadAsync(Scenes.Intro);
		}
	}
}