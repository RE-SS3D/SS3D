using JetBrains.Annotations;
using SS3D.Data.Generated;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace SS3D.SceneManagement
{
	public static class Scene
    {
        #if UNITY_EDITOR
        [NotNull]
        public static List<string> Names => new List<string>()
        {
            Scenes.Boot,
            Scenes.Intro,
            Scenes.Game,
            Scenes.Launcher,
        };
        #endif

		public static void Load(string scenes)
		{
			SceneManager.LoadScene(scenes);
		}

		public static async Task LoadAsync(string scene)
		{
			SceneManager.LoadSceneAsync(scene);
		}
	}
}