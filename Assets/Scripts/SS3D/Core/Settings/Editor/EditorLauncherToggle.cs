#if UNITY_EDITOR
using Coimbra;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace SS3D.Core.Settings.Editor
{
	[InitializeOnLoad]
	public sealed class EditorLauncherToggle
	{
		static EditorLauncherToggle()
		{
			ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
		}

		private static void OnToolbarGUI()
		{
			GUILayout.FlexibleSpace();

			GUILayoutOption[] options =
			{
				GUILayout.Width(175),
				GUILayout.Height(20),
			};

			ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

			bool forceLauncher = applicationSettings.ForceLauncher;

			string label = forceLauncher ? "Launcher enabled" : "Launcher disabled";

			if (GUILayout.Button(new GUIContent(label), options))
			{
				applicationSettings.ForceLauncher = !applicationSettings.ForceLauncher;
			}
		}
	}
}
#endif