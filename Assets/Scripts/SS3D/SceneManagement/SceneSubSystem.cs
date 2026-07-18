using Coimbra;
using Coimbra.Services.Events;
using SS3D.Application;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;
using SS3D.Data.Generated;
using SS3D.Logging;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
namespace SS3D.SceneManagement
{
    /// <summary>
    /// Used as a simple scene loader, all the current stuff works in the Game scene but this is used to go from the boot to the launcher scene
    /// </summary>
	public sealed class SceneSubSystem : SubSystem
	{
		protected override void OnAwake()
		{
			base.OnAwake();

			ApplicationInitializing.AddListener(HandleApplicationInitializing);
			UnitySceneManager.sceneLoaded += HandleUnitySceneLoaded;
		}

		protected override void OnDestroyed()
		{
			UnitySceneManager.sceneLoaded -= HandleUnitySceneLoaded;
			base.OnDestroyed();
		}

		private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
		{
			LoadMainScene();
		}

		/// <summary>
		/// FishNet loads Game additively and cannot unload Intro while it is still the only scene,
		/// so Intro (EventSystem + AudioListener) would otherwise stick around and spam the console.
		/// </summary>
		private void HandleUnitySceneLoaded(UnityScene scene, LoadSceneMode mode)
		{
			if (scene.name != Scenes.Game)
			{
				return;
			}

			UnitySceneManager.SetActiveScene(scene);
			// Unload is async — disable Intro/Launcher EventSystems immediately so they
			// do not coexist with Game's EventSystem for one or more frames.
			DisableEventSystemsInScene(Scenes.Intro);
			DisableEventSystemsInScene(Scenes.Launcher);
			UnloadIfLoaded(Scenes.Intro);
			UnloadIfLoaded(Scenes.Launcher);
		}

		private static void DisableEventSystemsInScene(string sceneName)
		{
			UnityScene loaded = UnitySceneManager.GetSceneByName(sceneName);
			if (!loaded.IsValid() || !loaded.isLoaded)
			{
				return;
			}

			foreach (GameObject root in loaded.GetRootGameObjects())
			{
				UnityEngine.EventSystems.EventSystem[] eventSystems =
					root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true);
				for (int i = 0; i < eventSystems.Length; i++)
				{
					eventSystems[i].enabled = false;
				}
			}
		}

		private static void UnloadIfLoaded(string sceneName)
		{
			UnityScene loaded = UnitySceneManager.GetSceneByName(sceneName);
			if (loaded.IsValid() && loaded.isLoaded)
			{
				UnitySceneManager.UnloadSceneAsync(loaded);
			}
		}

        /// <summary>
        /// Starts the launcher or the game, depending if the game was opened without or with command line args.
        /// </summary>
        private void LoadMainScene()
        {
#if UNITY_SERVER
            Log.Debug(this, "Loading main scene as Game (dedicated server)", Logs.Important);

            // This call is async and not awaited. Hence the pragma disable.
            #pragma warning disable CS4014
            Scene.LoadAsync(Scenes.Game);
            #pragma warning restore CS4014

            return;
#else
            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            bool isUsingCommandLineArgs = false;

            if (!UnityEngine.Application.isEditor)
            {
                isUsingCommandLineArgs = Environment.GetCommandLineArgs().Length > 1;
            }

            string sceneToLoad = isUsingCommandLineArgs ? Scenes.Intro : Scenes.Launcher;

            Log.Debug(this, $"Loading main scene as {sceneToLoad}", Logs.Important);

            if (applicationSettings.ForceLauncher)
            {
                sceneToLoad = Scenes.Launcher;
            }
            else if (!isUsingCommandLineArgs)
            {
                if (UnityEngine.Application.isEditor && !applicationSettings.ForceLauncher)
                {
                    sceneToLoad = Scenes.Intro;
                }
                else
                {
                    sceneToLoad = Scenes.Launcher;
                }
            }

            // This call is async and not awaited. Hence the pragma disable.
			#pragma warning disable CS4014
			Scene.LoadAsync(sceneToLoad);
			#pragma warning restore CS4014
#endif
		}
	}
}
