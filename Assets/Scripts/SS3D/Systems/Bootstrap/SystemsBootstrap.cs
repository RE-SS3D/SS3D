using SS3D.Application;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.SceneManagement;
using SS3D.Systems.Audio;
using SS3D.Systems.Inputs;
using SS3D.Systems.Rounds;
using SS3D.Systems.ScreenEffects;
using SS3D.Systems.Settings;
using SS3D.Systems.Testing;
using SS3D.Systems.Vision;
using SS3D.Systems.WorldReadiness;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SS3D.Systems.Bootstrap
{
    /// <summary>
    /// Single cold-start owner for process-wide Systems-layer services. Replaces scattered
    /// <c>RuntimeInitializeOnLoadMethod</c> self-bootstraps for those types and the five Boot.unity
    /// Persistent Systems. UI hosts (UiShell / MainHud / StoragePanel) still self-bootstrap until
    /// UiShell consolidation. <c>NetworkSessionSubSystem</c> / <c>CommandLineArgsSubSystem</c> are
    /// created here via assembly-qualified type names to avoid asmdef cycles.
    /// </summary>
    public static class SystemsBootstrap
    {
        private const string NetworkSessionTypeName =
            "SS3D.Networking.NetworkSessionSubSystem, SS3D.Networking";

        private const string CommandLineArgsTypeName =
            "SS3D.CommandLine.CommandLineArgsSubSystem, SS3D.CommandLine";

        /// <summary>Client main menu; by type name because SS3D.Systems does not reference SS3D.Launcher.</summary>
        private const string MainMenuTypeName =
            "SS3D.Launcher.MainMenuSubSystem, SS3D.Launcher";

        /// <summary>Scene the Unity Test Runner enters Play Mode in (InitTestScene&lt;guid&gt;).</summary>
        private const string TestRunnerScenePrefix = "InitTestScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Enter Play Mode runs without a domain reload, so a handler from an aborted run survives.
            SceneManager.sceneLoaded -= StartAfterTestRunnerScene;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            // PlayMode tests start in the Test Runner's scene and load Boot themselves. Cold-starting
            // here would load Intro over the runner's scene and the run would never start a test.
            if (SceneManager.GetActiveScene().name.StartsWith(TestRunnerScenePrefix, StringComparison.Ordinal))
            {
                SceneManager.sceneLoaded += StartAfterTestRunnerScene;
                return;
            }

            EnsureProcessWideServices();
        }

        private static void StartAfterTestRunnerScene(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (scene.name.StartsWith(TestRunnerScenePrefix, StringComparison.Ordinal))
            {
                return;
            }

            SceneManager.sceneLoaded -= StartAfterTestRunnerScene;
            EnsureProcessWideServices();
        }

        public static void EnsureProcessWideServices()
        {
            // Listeners for ApplicationPreInitializing / ApplicationInitializing must Awake before
            // ApplicationInitializerSubSystem.OnStart. Create ApplicationInitializer last in this batch.
            EnsureSubSystemType(NetworkSessionTypeName);
            EnsureSubSystemType(CommandLineArgsTypeName);
            // Saved player settings are applied before the intro and lobby (2026-10_player-settings).
            EnsureSubSystem<PlayerSettingsSubSystem>();
            EnsureSubSystem<SceneSubSystem>();
            EnsureSubSystem<InputSubSystem>();
            EnsureSubSystem<WorldReadinessSubSystem>();
            EnsureSubSystem<ScreenEffectsSubSystem>();
            EnsureSubSystem<AmbienceSubSystem>();
            EnsureSubSystem<PersonalAudioSubSystem>();
            EnsureSubSystem<AutomationSubSystem>();
            EnsureSubSystem<VisionSubSystem>();
            EnsureSubSystem<ServerStatusPublisher>();
#if !UNITY_SERVER
            EnsureSubSystemType(MainMenuTypeName);
#endif
            EnsureSubSystem<ApplicationInitializerSubSystem>();
        }

        public static T EnsureSubSystem<T>() where T : SubSystem
        {
            if (SubSystems.TryGet(out T existing))
            {
                return existing;
            }

            T found = UnityEngine.Object.FindObjectOfType<T>(true);
            if (found != null)
            {
                return found;
            }

            GameObject host = new(typeof(T).Name);
            UnityEngine.Object.DontDestroyOnLoad(host);
            return host.AddComponent<T>();
        }

        private static void EnsureSubSystemType(string assemblyQualifiedName)
        {
            Type type = Type.GetType(assemblyQualifiedName);
            if (type == null || !typeof(SubSystem).IsAssignableFrom(type))
            {
                Debug.LogError($"SystemsBootstrap: could not resolve SubSystem type '{assemblyQualifiedName}'");
                return;
            }

            UnityEngine.Object existing = UnityEngine.Object.FindObjectOfType(type, true);
            if (existing != null)
            {
                return;
            }

            GameObject host = new(type.Name);
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent(type);
        }
    }
}
