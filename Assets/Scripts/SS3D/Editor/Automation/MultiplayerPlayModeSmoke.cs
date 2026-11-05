using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Runs a multiplayer smoke scenario inside Unity Multiplayer Play Mode: the main editor hosts
    /// and clones Player2..4 join as clients, each running the automation script its
    /// <c>smoke:</c> tag names (see <c>SS3D.Application.MultiplayerPlayModePlayer</c>). Each player
    /// writes its result to <c>Temp/mppm-automation/&lt;player&gt;.json</c>.
    /// </summary>
    /// <remarks>
    /// MPPM's player control is internal to <c>UnityEditor.MultiplayerModule</c> (Unity 6000.3),
    /// so this reaches it by reflection and may break on an editor upgrade. Driven step by step by
    /// <c>Testing/ui/validate.sh --steps mppm:&lt;scenario&gt;</c>, because entering Play Mode
    /// and clone launches outlive a single call.
    /// </remarks>
    public static class MultiplayerPlayModeSmoke
    {
        private const string TagPrefix = "smoke:";
        private const string BootScenePath = "Assets/Content/Scenes/Boot.unity";
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        public static string ResultDirectory => Path.Combine(Directory.GetCurrentDirectory(), "Temp", "mppm-automation");

        /// <summary>
        /// Tags the main editor with <paramref name="hostScript"/> and one clone per client script,
        /// activates those clones and deactivates the rest. Returns an error, or empty.
        /// </summary>
        public static string Prepare(string hostScript, params string[] clientScripts)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return "in Play Mode";
            }

            if (clientScripts.Length > 3)
            {
                return "MPPM has at most three clones";
            }

            // Play starts from the open scene, and every other one has no NetworkManager: the host
            // never comes up and the clones sit at the launcher. Clones follow this scene.
            Scene openScene = SceneManager.GetActiveScene();
            if (openScene.path != BootScenePath)
            {
                if (openScene.isDirty)
                {
                    return $"{openScene.path} has unsaved changes; Play must start from Boot";
                }

                EditorSceneManager.OpenScene(BootScenePath);
            }

            if (Directory.Exists(ResultDirectory))
            {
                Directory.Delete(ResultDirectory, true);
            }

            IList players = Players();
            string[] scripts = new[] { hostScript }.Concat(clientScripts).ToArray();

            for (int i = 0; i < players.Count; i++)
            {
                object player = players[i];
                string error = ClearSmokeTags(player);

                if (error != null)
                {
                    return error;
                }

                if (i < scripts.Length)
                {
                    error = AddTag(player, TagPrefix + scripts[i]);

                    if (error != null)
                    {
                        return error;
                    }
                }

                if (i == 0)
                {
                    continue;
                }

                bool wanted = i < scripts.Length;
                bool launched = !Equals(Get(player, "PlayerState").ToString(), "NotLaunched");

                if (wanted && !launched)
                {
                    error = Invoke(player, "Activate", new List<string>());
                }
                else if (!wanted && launched)
                {
                    error = Invoke(player, "Deactivate");
                }

                if (error != null)
                {
                    return $"{Get(player, "Name")}: {error}";
                }
            }

            return string.Empty;
        }

        /// <summary>One line per player: name, launch state and tags.</summary>
        public static string Status()
        {
            return string.Join("\n", Players().Cast<object>().Select(player =>
                $"{Get(player, "Name")}|{Get(player, "PlayerState")}|{string.Join(",", (string[])Get(player, "Tags"))}"));
        }

        /// <summary>Leaves Play Mode, removes the smoke tags and optionally closes the clones.</summary>
        public static string Finish(bool deactivateClones)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.ExitPlaymode();
                return "exiting Play Mode; call again";
            }

            IList players = Players();

            for (int i = 0; i < players.Count; i++)
            {
                ClearSmokeTags(players[i]);

                if (i > 0 && deactivateClones && !Equals(Get(players[i], "PlayerState").ToString(), "NotLaunched"))
                {
                    Invoke(players[i], "Deactivate");
                }
            }

            return string.Empty;
        }

        private static IList Players()
        {
            Type playmode = Mppm("Unity.Multiplayer.PlayMode.Editor.MultiplayerPlaymode");

            return (IList)playmode.GetProperty("Players", Any).GetValue(null)
                ?? throw new InvalidOperationException("Multiplayer Play Mode is not initialized (is it enabled in Project Settings?)");
        }

        private static string ClearSmokeTags(object player)
        {
            foreach (string tag in (string[])Get(player, "Tags"))
            {
                if (!tag.StartsWith(TagPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string error = Invoke(player, "RemoveTag", tag);

                if (error != null)
                {
                    return error;
                }
            }

            return null;
        }

        private static string AddTag(object player, string tag)
        {
            object registry = Mppm("Unity.Multiplayer.PlayMode.Editor.MultiplayerPlaymode").GetProperty("PlayerTags", Any).GetValue(null);
            object[] args = { tag, null };

            // Registering an existing tag reports Duplicate; that's fine.
            registry.GetType().GetMethod("Add", Any).Invoke(registry, args);

            return Invoke(player, "AddTag", tag);
        }

        /// <summary>Calls a bool-returning MPPM method whose last out parameter is an error enum.</summary>
        private static string Invoke(object target, string method, params object[] leading)
        {
            MethodInfo info = target.GetType().GetMethod(method, Any)!;
            ParameterInfo[] parameters = info.GetParameters();
            object[] args = new object[parameters.Length];
            int errorIndex = Array.FindIndex(parameters, parameter => parameter.ParameterType.IsByRef);

            for (int i = 0, j = 0; i < parameters.Length; i++)
            {
                if (i == errorIndex)
                {
                    continue;
                }

                args[i] = j < leading.Length ? leading[j++] : null;
            }

            bool ok = (bool)info.Invoke(target, args);

            return ok ? null : $"{method} failed: {args[errorIndex]}";
        }

        private static object Get(object target, string property) => target.GetType().GetProperty(property, Any).GetValue(target);

        private static Type Mppm(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(type => type != null)
                ?? throw new InvalidOperationException($"{typeName} not found; MPPM internals changed with this editor version");
        }
    }
}
