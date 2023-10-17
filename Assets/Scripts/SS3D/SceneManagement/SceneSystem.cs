using Coimbra;
using Coimbra.Services.Events;
using SS3D.Application;
using SS3D.Application.Events;
using SS3D.Data.Enums;
using SS3D.Logging;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
namespace SS3D.SceneManagement
{
	public sealed class SceneSystem : Core.Behaviours.System
	{
		protected override void OnAwake()
		{
			base.OnAwake();

			ApplicationInitializing.AddListener(HandleApplicationInitializing);
		}

		private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
		{
			LoadMainScene();
		}

		/// <summary>
		/// Starts the launcher or the game, depending if the game was opened without or with command line args.
		/// </summary>
		private void LoadMainScene()
		{
			ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();
			bool isUsingCommandLineArgs = 
#if UNITY_EDITOR
				true;
#else
                Environment.GetCommandLineArgs().Length > 1;
#endif
			Scenes sceneToLoad = isUsingCommandLineArgs ? Scenes.Intro : Scenes.Launcher;

            Log.Information(this, $"Loading main scene as {sceneToLoad}", Logs.Important);

			if (applicationSettings.ForceLauncher || !isUsingCommandLineArgs)
			{
				sceneToLoad = Scenes.Launcher;
			}                                  

			// This call is async and not awaited. Hence the pragma disable.
			#pragma warning disable CS4014
			Scene.LoadAsync(sceneToLoad);
			#pragma warning restore CS4014
		}
	}
}