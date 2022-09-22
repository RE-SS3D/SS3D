#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;

namespace SS3D.Utils.Editor
{
	[InitializeOnLoad]
	public sealed class SceneSwitchLeftButton
	{
		static SceneSwitchLeftButton()
		{
			ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
		}

		private static void OnToolbarGUI()
		{
			GUILayout.FlexibleSpace();

			if (GUILayout.Button(new GUIContent("Startup", "Load Scene Startup, use this to start the game"),
				    new[] { GUILayout.Width(65), GUILayout.Height(19) }))
			{
				SceneHelper.StartScene("Startup");
			}

			if (GUILayout.Button(new GUIContent("Lobby", "Load Scene Lobby, use this to develop in-game stuff"),
				    new[] { GUILayout.Width(65), GUILayout.Height(19) }))
			{
				SceneHelper.StartScene("Lobby");
			}
		}
	}

	public static class SceneHelper
	{
		private static string _sceneToOpen;

		public static void StartScene(string sceneName)
		{
			if (EditorApplication.isPlaying)
			{
				EditorApplication.isPlaying = false;
			}

			_sceneToOpen = sceneName;
			EditorApplication.update += OnUpdate;
		}

		private static void OnUpdate()
		{
			if (_sceneToOpen == null ||
			    EditorApplication.isPlaying || EditorApplication.isPaused ||
			    EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}

			EditorApplication.update -= OnUpdate;

			if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				// need to get scene via search because the path to the scene
				// file contains the package version so it'll change over time
				string[] guids = AssetDatabase.FindAssets("t:scene " + _sceneToOpen, null);
				if (guids.Length == 0)
				{
					Debug.LogWarning("Couldn't find scene file");
				}
				else
				{
					string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
					EditorSceneManager.OpenScene(scenePath);
				}
			}

			_sceneToOpen = null;
		}
	}
}
#endif