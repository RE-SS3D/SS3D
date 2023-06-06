#if UNITY_EDITOR
using SS3D.Data.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SS3D.SceneManagement.Editor
{
	public static class EditorSceneUtils
	{
		private static string _sceneToOpen;

		public static void StartScene(Scenes sceneName)
		{ 
			StartScene(sceneName.ToString());
			EditorApplication.update += OnUpdate;
		}

		private static void StartScene(string sceneName)
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