using System;
using System.IO;
using Coimbra;
using FishNet;
using SS3D.Application;
using SS3D.Core;
using SS3D.Networking;
using SS3D.Networking.Settings;
using SS3D.Systems.Entities;
using SS3D.Systems.Screens;
using SS3D.Systems.Vision;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SessionState = UnityEditor.SessionState;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Rendering look-dev round: one menu item enters Play Mode as Host, runs <see cref="ScriptPath"/> through
    /// <c>AutomationSubSystem</c> (ready, start the round, embark), then pulls the player camera out to an overview and
    /// turns the line-of-sight mask off, so the station look can be tuned live and judged from Game view captures.
    /// Everything it changes in the editor and at runtime is put back when Play Mode exits.
    /// Runbook: <c>Testing/ui/README.md</c>, "Station look-dev round".
    /// </summary>
    [InitializeOnLoad]
    public static partial class StationLookDev
    {
        /// <summary>The automation script the round runs, relative to the project root.</summary>
        public const string ScriptPath = "Testing/ui/lookdev-round.txt";

        /// <summary>Camera distance for the overview. The zoom wheel stops at 15; <see cref="Frame"/> accepts more.</summary>
        public const float OverviewDistance = 15f;

        /// <summary>Degrees above the body for the overview. CameraFollow clamps this to 10–80 every frame.</summary>
        public const float OverviewVerticalAngle = 55f;

        private const string BootScenePath = "Assets/Content/Scenes/Boot.unity";

        // The script waits up to 180 s per step, and a station load can take two of them.
        private const double EmbarkTimeoutSeconds = 420;

        // Lets the camera finish easing out to the overview and the AO history settle before Ready.
        private const double SettleSeconds = 4;
        private const double PlayStartTimeoutSeconds = 15;
        private const double ResultPollSeconds = 0.5;

        private const string ActiveKey = "SS3D.StationLookDev.Active";
        private const string SceneKey = "SS3D.StationLookDev.PreviousStartScene";
        private const string SizeKey = "SS3D.StationLookDev.PreviousGameViewSize";
        private const string ScriptKey = "SS3D.StationLookDev.PreviousTestScriptPath";
        private const string NetworkTypeKey = "SS3D.StationLookDev.PreviousNetworkType";
        private const string StackTraceKey = "SS3D.StationLookDev.PreviousWarningStackTrace";

        private static double Deadline;
        private static double ReadyAt;
        private static double NextResultPoll;

        static StationLookDev()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            // A recompile or crash between Start and the end of Play must not leave the next Play running the script.
            if (SessionState.GetBool(ActiveKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RestoreEditorSettings();
            }
        }

        public enum Phase
        {
            Idle = 0,
            Starting = 1,
            Embarking = 2,
            Settling = 3,
            Ready = 4,
            Failed = 5,
        }

        /// <summary>
        /// Where the round is. Capture once it reads <see cref="Phase.Ready"/>; from the Unity CLI, wait on the member
        /// <c>SS3D.Editor.Automation.StationLookDev.State</c> with <c>unity command job wait</c>.
        /// </summary>
        public static Phase State { get; private set; }

        /// <summary>Why the last round reached <see cref="Phase.Failed"/>; empty otherwise.</summary>
        public static string Failure { get; private set; } = string.Empty;

        /// <summary>True from <see cref="Start"/> until Play exits and the editor settings are back.</summary>
        public static bool IsActive => SessionState.GetBool(ActiveKey, false);

        /// <summary>
        /// Enters Play Mode as Host on a round the automation script starts and embarks into. Refuses in Play Mode or
        /// with compile errors. Restores the test script path, network type, Play Mode start scene, Game view size and
        /// warning stack traces (AutomationSubSystem sets them to Full) when Play Mode exits.
        /// </summary>
        [MenuItem("SS3D/Testing/Start Station Look-Dev Round")]
        public static void Start()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before starting a look-dev round.");
            }

            if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Fix compilation errors before starting a look-dev round.");
            }

            string script = Path.GetFullPath(ScriptPath);
            if (!File.Exists(script))
            {
                throw new FileNotFoundException("The look-dev automation script is missing.", script);
            }

            if (SessionState.GetBool(ActiveKey, false))
            {
                RestoreEditorSettings();
            }

            ApplicationSettings application = ScriptableSettings.GetOrFind<ApplicationSettings>();
            NetworkSettings network = ScriptableSettings.GetOrFind<NetworkSettings>();

            SessionState.SetString(ScriptKey, application.TestScriptPath ?? string.Empty);
            SessionState.SetInt(NetworkTypeKey, (int)network.NetworkType);
            SessionState.SetInt(StackTraceKey, (int)UnityEngine.Application.GetStackTraceLogType(LogType.Warning));
            SessionState.SetString(SceneKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetInt(SizeKey, AutomationGameView.Select1080p());
            SessionState.SetBool(ActiveKey, true);

            // In memory only: neither settings asset is marked dirty, and both go back on Play exit. With a test script
            // set, SceneSubSystem loads Game directly (no Intro or Launcher) and AutomationSubSystem runs the script.
            application.TestScriptPath = script;
            network.NetworkType = NetworkType.Host;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);

            DeleteScriptResult();
            Failure = string.Empty;
            State = Phase.Starting;
            Deadline = EditorApplication.timeSinceStartup + PlayStartTimeoutSeconds;
            EditorApplication.update += WatchPlayStart;
            EditorApplication.isPlaying = true;
        }

        /// <summary>
        /// Re-frames the player camera on its body: <paramref name="distance"/> on CameraFollow's exponential scale
        /// (the zoom wheel's 3–15), <paramref name="verticalAngle"/> degrees above it (clamped to 10–80 by CameraFollow).
        /// Play Mode only; the camera eases there over a second or two.
        /// </summary>
        public static string Frame(float distance, float verticalAngle)
        {
            if (!TryGetFollowedBody(out CameraFollow follow))
            {
                if (SceneRegistry<CameraFollow>.Count == 0)
                {
                    throw new InvalidOperationException("No player camera to frame; enter Play Mode and embark first.");
                }

                follow = SceneRegistry<CameraFollow>.All[0];
            }

            SerializedObject serialized = new(follow);
            Property(serialized, "_cameraDistance").floatValue = distance;
            Property(serialized, "_verticalAngle").floatValue = verticalAngle;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return $"Camera framed at distance {distance}, {verticalAngle}° above the body.";
        }

        /// <summary>
        /// Turns the line-of-sight (FOV) mask back on or off for the rest of the round. The look-dev round starts
        /// with it off so whole rooms show.
        /// </summary>
        public static string SetLineOfSight(bool enabled)
        {
            // VisionSubSystem owns the vision state (systems/fov.md State); without it there is no mask to switch.
            if (!SubSystems.TryGet(out VisionSubSystem vision))
            {
                return "No vision subsystem yet; line of sight unchanged.";
            }

            vision.SetSuppressed(!enabled);
            return enabled ? "Line of sight on." : "Line of sight off.";
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false))
            {
                return;
            }

            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                {
                    EditorApplication.update -= WatchPlayStart;
                    Deadline = EditorApplication.timeSinceStartup + EmbarkTimeoutSeconds;
                    NextResultPoll = 0;
                    State = Phase.Embarking;
                    EditorApplication.update -= Tick;
                    EditorApplication.update += Tick;
                    break;
                }

                case PlayModeStateChange.ExitingPlayMode:
                {
                    EditorApplication.update -= Tick;
                    ReleaseRuntime();
                    break;
                }

                case PlayModeStateChange.EnteredEditMode:
                {
                    RestoreEditorSettings();
                    break;
                }
            }
        }

        // Entering Play can be refused (compile errors, a modal); without Play there is no exit to restore on.
        private static void WatchPlayStart()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < Deadline)
            {
                return;
            }

            EditorApplication.update -= WatchPlayStart;
            if (SessionState.GetBool(ActiveKey, false))
            {
                RestoreEditorSettings();
                Fail("Play Mode did not start.");
            }
        }

        private static void Tick()
        {
            try
            {
                double now = EditorApplication.timeSinceStartup;

                switch (State)
                {
                    case Phase.Embarking:
                    {
                        if (now >= NextResultPoll)
                        {
                            NextResultPoll = now + ResultPollSeconds;
                            string scriptFailure = ReadScriptFailure();
                            if (scriptFailure != null)
                            {
                                Fail($"The look-dev script failed: {scriptFailure}");
                                return;
                            }
                        }

                        if (now > Deadline)
                        {
                            Fail($"No embarked player after {EmbarkTimeoutSeconds} s.");
                            return;
                        }

                        if (!TryGetFollowedBody(out _))
                        {
                            return;
                        }

                        Frame(OverviewDistance, OverviewVerticalAngle);
                        SetLineOfSight(false);
                        ReadyAt = now + SettleSeconds;
                        State = Phase.Settling;
                        break;
                    }

                    case Phase.Settling:
                    {
                        if (now < ReadyAt)
                        {
                            return;
                        }

                        State = Phase.Ready;
                        Debug.Log("[look-dev] Ready: hosted round, player embarked, overview camera, line of sight off. "
                            + "Capture with `unity command capture game_view --output-path <png>`; exit Play Mode to undo.");
                        EditorApplication.update -= Tick;
                        break;
                    }

                    default:
                    {
                        EditorApplication.update -= Tick;
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                Fail(exception.ToString());
            }
        }

        /// <summary>True once this editor's client owns a spawned body and the player camera follows it.</summary>
        private static bool TryGetFollowedBody(out CameraFollow follow)
        {
            follow = null;

            // EntitySubSystem exists only once Game is up, so FishNet is ready by the time it is read.
            if (!SubSystems.TryGet(out EntitySubSystem entities)
                || !entities.TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity body)
                || body == null)
            {
                return false;
            }

            foreach (CameraFollow candidate in SceneRegistry<CameraFollow>.All)
            {
                SerializedObject serialized = new(candidate);
                if (Property(serialized, "_target").objectReferenceValue is GameObject target
                    && (target == body.gameObject || target.transform.IsChildOf(body.transform)))
                {
                    follow = candidate;
                    return true;
                }
            }

            return false;
        }

        private static SerializedProperty Property(SerializedObject serialized, string name)
        {
            return serialized.FindProperty(name)
                ?? throw new MissingFieldException(serialized.targetObject.GetType().Name, name);
        }

        private static void ReleaseRuntime()
        {
            // The suppression lives on VisionSubSystem, which Play exit destroys; clear it in case the round outlives this.
            if (SubSystems.TryGet(out VisionSubSystem vision))
            {
                vision.SetSuppressed(false);
            }
        }

        private static void RestoreEditorSettings()
        {
            // The script path first: a stale one makes every later Play run the look-dev round.
            ApplicationSettings application = ScriptableSettings.GetOrFind<ApplicationSettings>();
            string previousScript = SessionState.GetString(ScriptKey, string.Empty);
            application.TestScriptPath = previousScript == Path.GetFullPath(ScriptPath) ? string.Empty : previousScript;

            NetworkSettings network = ScriptableSettings.GetOrFind<NetworkSettings>();
            network.NetworkType = (NetworkType)SessionState.GetInt(NetworkTypeKey, (int)network.NetworkType);

            // AutomationSubSystem switches warnings to full stack traces, which outlives Play Mode in the editor.
            UnityEngine.Application.SetStackTraceLogType(LogType.Warning,
                (StackTraceLogType)SessionState.GetInt(StackTraceKey, (int)UnityEngine.Application.GetStackTraceLogType(LogType.Warning)));

            string previousScene = SessionState.GetString(SceneKey, string.Empty);
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previousScene)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousScene);

            SessionState.SetBool(ActiveKey, false);
            SessionState.EraseString(ScriptKey);
            SessionState.EraseInt(NetworkTypeKey);
            SessionState.EraseInt(StackTraceKey);
            SessionState.EraseString(SceneKey);

            if (State != Phase.Failed)
            {
                State = Phase.Idle;
            }

            try
            {
                AutomationGameView.Restore(SessionState.GetInt(SizeKey, 0));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[look-dev] Could not restore the Game view size: {exception.Message}");
            }

            SessionState.EraseInt(SizeKey);
        }

        private static string ResultPath =>
            Path.Combine(MultiplayerPlayModePlayer.ResultDirectory, MultiplayerPlayModePlayer.Name + ".json");

        private static void DeleteScriptResult()
        {
            if (File.Exists(ResultPath))
            {
                File.Delete(ResultPath);
            }
        }

        /// <summary>AutomationSubSystem writes its outcome here in the editor; returns the reason if it failed.</summary>
        private static string ReadScriptFailure()
        {
            if (!File.Exists(ResultPath))
            {
                return null;
            }

            ScriptResult result = JsonUtility.FromJson<ScriptResult>(File.ReadAllText(ResultPath));
            return result.signal == "ScriptFailed" ? result.reason : null;
        }

        private static void Fail(string reason)
        {
            EditorApplication.update -= Tick;
            Failure = reason;
            State = Phase.Failed;
            Debug.LogError($"[look-dev] {reason}");
        }

        [Serializable]
        private sealed class ScriptResult
        {
            public string signal = string.Empty;
            public string reason = string.Empty;
        }
    }
}
