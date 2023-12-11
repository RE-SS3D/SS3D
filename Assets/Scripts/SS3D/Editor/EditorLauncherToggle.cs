#if UNITY_EDITOR
using Coimbra;
using SS3D.Application;
using SS3D.Networking;
using SS3D.Networking.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;
using Random = UnityEngine.Random;

namespace SS3D.Editor
{
	[InitializeOnLoad]
	public sealed class EditorLauncherToggle
	{
		static EditorLauncherToggle()
		{
			ToolbarExtender.RightToolbarGUI.Add(OnRightToolbarGUI);
		}
		private static void OnRightToolbarGUI()
		{
			if (UnityEngine.Application.isPlaying)
			{
				return;
			}

			GUILayout.FlexibleSpace();

			GUILayoutOption[] options =
			{
				GUILayout.Width(135),
				GUILayout.Height(20),
			};

			ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

			bool forceLauncher = applicationSettings.ForceLauncher;

			string label = forceLauncher ? "Launcher enabled" : "Launcher disabled";

			if (GUILayout.Button(new GUIContent(label), options))
			{
				applicationSettings.ForceLauncher = !applicationSettings.ForceLauncher;
			}

			GUILayout.FlexibleSpace();

			GUILayoutOption[] networkModeOptions =
			{
				GUILayout.Width(215),
				GUILayout.Height(20),
			};

			GUILayoutOption[] randomPortOptions =
			{
				GUILayout.Width(125),
				GUILayout.Height(20),
			};

			NetworkSettings networkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();

			NetworkType networkType = networkSettings.NetworkType;
			
			List<string> array = Enum.GetNames(typeof(NetworkType)).ToList();

			if (GUILayout.Button(new GUIContent("Network mode: " + networkType), networkModeOptions))
			{
				int indexOf = array.IndexOf(networkType.ToString());

				if (indexOf >= array.Count - 1)
				{
					indexOf = 0;
				}
				else
				{
					indexOf++;
				}

				networkType = Enum.Parse<NetworkType>(array[indexOf]);
				networkSettings.NetworkType = networkType;
			}

			if (GUILayout.Button(new GUIContent("Server Port: " + networkSettings.ServerPort), randomPortOptions))
			{
				networkSettings.ServerPort = (ushort)Random.Range(1000, 10000);
			}
		}
	}
}
#endif