using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Non-interactive client and dedicated-server builds for agents: refuses on a dirty/polluted scene
    /// instead of letting BuildPlayer open a modal dialog, returns before the build blocks the main
    /// thread, and reports through artifacts/ui/{client,server}-build-result.json. Builds are stamped
    /// with Testing/ui/source_hash.py so stale builds are detected.
    /// <para>From MCP: <c>execute_code</c> with <c>SS3D.Editor.Automation.AutomationBuild.BuildClient();</c>
    /// or <c>BuildServer();</c></para>
    /// </summary>
    public static class AutomationBuild
    {
        public const string ClientBinary = "Builds/Game/SS3D.x86_64";
        public const string HashFile = "Builds/Game/ss3d-build-hash.txt";
        public const string ResultFile = "artifacts/ui/client-build-result.json";
        public const string ServerBinary = "Builds/GameServer/SS3D.x86_64";
        public const string ServerHashFile = "Builds/GameServer/ss3d-build-hash.txt";
        public const string ServerResultFile = "artifacts/ui/server-build-result.json";

        private sealed class Target
        {
            public string Name;
            public string Binary;
            public string HashFile;
            public string ResultFile;
            public Action<string> Build;
        }

        private static readonly Target Client = new()
        {
            Name = "client", Binary = ClientBinary, HashFile = HashFile, ResultFile = ResultFile,
            Build = ClientBuildScript.BuildClient,
        };

        private static readonly Target Server = new()
        {
            Name = "server", Binary = ServerBinary, HashFile = ServerHashFile, ResultFile = ServerResultFile,
            Build = ServerBuildScript.BuildServer,
        };

        [Serializable]
        private sealed class Result
        {
            public string status;
            public double seconds;
            public string error;
            public string sourceHash;
            public string startedUtc;
        }

        /// <summary>Validates, then schedules the build on the next editor tick. Poll <see cref="ResultFile"/>.</summary>
        public static void BuildClient() => Schedule(Client);

        /// <summary>Dedicated server (UNITY_SERVER) build; poll <see cref="ServerResultFile"/>.</summary>
        public static void BuildServer() => Schedule(Server);

        /// <summary>Null when the client build matches the working tree, otherwise why it is stale.</summary>
        public static string ClientStaleReason() => StaleReason(Client);

        /// <summary>Null when the server build matches the working tree, otherwise why it is stale.</summary>
        public static string ServerStaleReason() => StaleReason(Server);

        private static void Schedule(Target target)
        {
            Result result = new() { status = "building", startedUtc = DateTime.UtcNow.ToString("o") };
            try
            {
                if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                    throw new InvalidOperationException($"Fix compilation errors before building the {target.Name}.");
                AutomationPreflight.Require(target.Name + " build");
            }
            catch (Exception exception)
            {
                result.status = "failed";
                result.error = exception.Message;
                Write(target, result);
                throw;
            }

            Write(target, result);
            // Not delayCall: an idle, unfocused editor can leave the delayCall queue unprocessed
            // indefinitely while update keeps ticking.
            void Once()
            {
                EditorApplication.update -= Once;
                Build(target, result);
            }
            EditorApplication.update += Once;
        }

        private static void Build(Target target, Result result)
        {
            Stopwatch watch = Stopwatch.StartNew();
            // The build scripts leave the editor on their subtarget; a Server subtarget would make the
            // editor compile with UNITY_SERVER, so put the previous one back.
            StandaloneBuildSubtarget subtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
            try
            {
                // Re-check: something may have dirtied a scene between the request and this tick.
                AutomationPreflight.Require(target.Name + " build");
                target.Build(target.Binary);
                // Hash after the build so files the build itself rewrites are part of the stamp.
                result.sourceHash = SourceHash();
                File.WriteAllText(target.HashFile, result.sourceHash + "\n");
                result.status = "succeeded";
            }
            catch (Exception exception)
            {
                result.status = "failed";
                result.error = exception.ToString();
            }
            finally
            {
                EditorUserBuildSettings.standaloneBuildSubtarget = subtarget;
            }
            result.seconds = watch.Elapsed.TotalSeconds;
            Write(target, result);
            Debug.Log($"[SS3D Automation] {char.ToUpper(target.Name[0]) + target.Name.Substring(1)} build {result.status} in {result.seconds:0.0}s");
        }

        private static string StaleReason(Target target)
        {
            if (!File.Exists(target.Binary)) return $"no {target.Name} build at {target.Binary}";
            if (!File.Exists(target.HashFile)) return $"{target.Name} build has no source stamp ({target.HashFile})";
            string built = File.ReadAllText(target.HashFile).Trim();
            string current = SourceHash();
            return built == current ? null : $"{target.Name} build is stale (built from {built}, working tree is {current})";
        }

        public static string SourceHash()
        {
            ProcessStartInfo info = new("python3", "Testing/ui/source_hash.py")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = Path.GetFullPath("."),
            };
            using Process process = Process.Start(info);
            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0 || output.Length == 0)
                throw new InvalidOperationException("Testing/ui/source_hash.py failed: " + error);
            return output;
        }

        private static void Write(Target target, Result result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target.ResultFile));
            File.WriteAllText(target.ResultFile, JsonUtility.ToJson(result, true));
        }
    }
}
