#if UNITY_EDITOR
using SS3D.Data.Enums;
using System;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace SS3D.SceneManagement.Editor
{
	[InitializeOnLoad]
	public sealed class EditorSceneSwitchLeftButton
	{
		static EditorSceneSwitchLeftButton()
		{
			ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
		}

		private static void OnToolbarGUI()
		{
			GUILayout.FlexibleSpace();

			GUILayout.Space(20);

			GUILayoutOption[] options =
			{
				GUILayout.Width(65),
				GUILayout.Height(20),
			};

			GUILayoutOption[] switcherLabelOptions =
			{
				GUILayout.Width(100),
				GUILayout.Height(20),
			};

			GUILayout.Label("Scene switcher:", switcherLabelOptions);

			foreach (string scene in Scene.Names)
			{
				if (GUILayout.Button(new GUIContent(scene), options))
				{
					Enum.TryParse(scene, out Scenes sceneEnum);

					EditorSceneUtils.StartScene(sceneEnum);
				}	
			}
		}
	}
}
#endif