using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Shared guard for builds and pilots: dirty open scenes make BuildPlayer block on a modal
    /// "Scene(s) Have Been Modified" dialog, and runtime objects leaked into an edit scene get saved
    /// by whoever clicks Save. Reports only; never saves or discards on the owner's behalf.
    /// </summary>
    public static class AutomationPreflight
    {
        // Root objects that only runtime code creates: tile chunks from a station load, preview booth parts.
        private static readonly string[] LeakPrefixes = { "Chunk [", "CharacterPreviewDummy", "CharacterPreviewStaging" };

        // Async work started in Play Mode (e.g. a station load) can still land a few frames after exit.
        private const double SettleSeconds = 3;

        /// <summary>Dirty open scenes and leaked root objects, one readable line each; empty when clean.</summary>
        public static string[] Check()
        {
            List<string> problems = new();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                string name = string.IsNullOrEmpty(scene.path) ? $"untitled scene '{scene.name}'" : scene.path;
                if (scene.isDirty) problems.Add($"Scene has unsaved changes: {name}");
                if (!scene.isLoaded) continue;
                string[] leaked = scene.GetRootGameObjects().Select(root => root.name)
                    .Where(rootName => LeakPrefixes.Any(rootName.StartsWith)).ToArray();
                if (leaked.Length > 0)
                    problems.Add($"Runtime objects in edit scene {name}: {string.Join(", ", leaked.Take(10))}"
                        + (leaked.Length > 10 ? $" (+{leaked.Length - 10} more)" : ""));
            }
            return problems.ToArray();
        }

        /// <summary>Throws with every problem if the editor is not safe for <paramref name="action"/>.</summary>
        public static void Require(string action)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException($"Exit Play Mode before the {action}.");
            string[] problems = Check();
            if (problems.Length > 0)
                throw new InvalidOperationException($"Preflight refused the {action}; the owner decides whether to save or discard:\n- "
                    + string.Join("\n- ", problems));
        }

        /// <summary>Runs <see cref="Check"/> once Play Mode has exited and late async work has settled.</summary>
        public static void AfterPlay(Action<string[]> done)
        {
            double until = EditorApplication.timeSinceStartup + SettleSeconds;
            void Wait()
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < until) return;
                EditorApplication.update -= Wait;
                string[] problems;
                try { problems = Check(); }
                catch (Exception exception) { problems = new[] { "Postcondition check threw: " + exception.Message }; }
                done(problems);
            }
            EditorApplication.update += Wait;
        }
    }
}
