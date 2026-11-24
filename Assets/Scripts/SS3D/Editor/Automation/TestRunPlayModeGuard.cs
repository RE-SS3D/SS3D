using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Holds Play Mode while a Test Runner job is still active, then starts it once the job ends.
    /// The EditMode runner allows domain reloads again as soon as its tests finish, but the job restores the
    /// owner's scene setup a few editor ticks later. Play pressed in that gap, or held back during the run and
    /// released into it, makes the restore throw "This cannot be used during play mode", and the editor keeps
    /// the Test Runner's scene instead of the owner's. Another session driving the editor over the CLI hits
    /// this when it presses Play during someone else's test run.
    /// </summary>
    /// <remarks>
    /// The Test Framework has no public signal for "job finished, scenes restored": RunFinished fires before the
    /// restore. This reads the internal <c>TestRunnerApi.IsRunActive()</c> and does nothing if a framework
    /// update removes it. A PlayMode test run enters Play from its saved bootstrap scene
    /// (<c>Assets/InitTestScene&lt;guid&gt;.unity</c>), which is let through. EditMode tests that yield
    /// <c>EnterPlayMode</c> would be held; none exist.
    /// </remarks>
    [InitializeOnLoad]
    internal static class TestRunPlayModeGuard
    {
        // SessionState survives the domain reload a recompile triggers mid-cleanup; the job resumes after it.
        private const string HeldKey = "SS3D.TestRunPlayModeGuard.PlayHeld";
        private const string PlayModeTestScenePrefix = "Assets/InitTestScene";

        private static readonly Func<bool> IsTestRunActive = FindIsRunActive();

        static TestRunPlayModeGuard()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (SessionState.GetBool(HeldKey, false))
            {
                EditorApplication.update -= StartHeldPlayModeWhenRunEnds;
                EditorApplication.update += StartHeldPlayModeWhenRunEnds;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode || !IsTestRunActive())
            {
                return;
            }

            if (SceneManager.GetActiveScene().path.StartsWith(PlayModeTestScenePrefix, StringComparison.Ordinal))
            {
                return;
            }

            EditorApplication.isPlaying = false;
            if (!SessionState.GetBool(HeldKey, false))
            {
                Debug.LogWarning("[TestRunPlayModeGuard] Play Mode held: a Test Runner job is still running and has not " +
                    "restored the open scenes yet. Play starts by itself when it ends; cancel the test run to start sooner.");
            }

            SessionState.SetBool(HeldKey, true);
            EditorApplication.update -= StartHeldPlayModeWhenRunEnds;
            EditorApplication.update += StartHeldPlayModeWhenRunEnds;
        }

        private static void StartHeldPlayModeWhenRunEnds()
        {
            if (IsTestRunActive() || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            EditorApplication.update -= StartHeldPlayModeWhenRunEnds;
            SessionState.EraseBool(HeldKey);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Debug.Log("[TestRunPlayModeGuard] Test Runner job finished; starting the held Play Mode.");
            EditorApplication.isPlaying = true;
        }

        private static Func<bool> FindIsRunActive()
        {
            Type api = Type.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi, UnityEditor.TestRunner");
            MethodInfo method = api?.GetMethod("IsRunActive", BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (method == null || method.ReturnType != typeof(bool))
            {
                Debug.LogWarning("[TestRunPlayModeGuard] TestRunnerApi.IsRunActive() not found; Play Mode is no longer held " +
                    "during test runs. Update the guard for the installed Test Framework.");
                return () => false;
            }

            return (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), method);
        }
    }
}
