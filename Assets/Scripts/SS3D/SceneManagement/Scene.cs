using JetBrains.Annotations;
using SS3D.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SS3D.SceneManagement
{
	public static class Scene
	{
		[NotNull]
		public static List<string> Names => Enum.GetNames(typeof(Scenes)).ToList();

		public static void Load(Scenes scenes)
		{
			SceneManager.LoadScene(scenes.ToString());
		}

		public static async Task LoadAsync(Scenes scene)
		{
			SceneManager.LoadSceneAsync(scene.ToString());
		}
	}
}