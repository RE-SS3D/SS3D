#if UNITY_EDITOR
using SS3D.Data.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SS3D.SceneManagement.Editor
{
	public static class EditorSceneUtils
	{
		private static string SceneToOpen;

		public static void StartScene(string sceneName)
		{
			EditorApplication.update += OnUpdate;

			if (EditorApplication.isPlaying)
			{
				EditorApplication.isPlaying = false;
			}

			SceneToOpen = sceneName;
			EditorApplication.update += OnUpdate;
		}

		private static void OnUpdate()
		{
			if (SceneToOpen == null || EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}

			EditorApplication.update -= OnUpdate;

			if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				// need to get scene via search because the path to the scene
				// file contains the package version so it'll change over time
				string[] guids = AssetDatabase.FindAssets("t:scene " + SceneToOpen, null);

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

			SceneToOpen = null;
		}
	}
}
#endif