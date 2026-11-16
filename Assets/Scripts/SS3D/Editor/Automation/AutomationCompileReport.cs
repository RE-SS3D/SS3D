using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Writes every compilation's errors (compiler and analyzer, e.g. COIMBRA*) to
    /// artifacts/ui/compile-result.json, so Testing/ui/compile.sh waits on a file stamped after its
    /// request rather than on console filters or DLL timestamps. A failed compile keeps the old domain
    /// loaded, so this hook still reports errors in its own assembly.
    /// </summary>
    [InitializeOnLoad]
    public static class AutomationCompileReport
    {
        private static readonly List<string> Errors = new();
        private static readonly List<string> FailedAssemblies = new();
        private static long _started;

        [Serializable]
        private sealed class Report
        {
            public long startedUnixMs;
            public long finishedUnixMs;
            public bool failed;
            public string[] failedAssemblies;
            public string[] errors;
        }

        static AutomationCompileReport()
        {
            CompilationPipeline.compilationStarted += _ =>
            {
                Errors.Clear();
                FailedAssemblies.Clear();
                _started = Now();
            };
            CompilationPipeline.assemblyCompilationFinished += (assembly, messages) =>
            {
                bool failed = false;
                foreach (CompilerMessage message in messages)
                {
                    if (message.type != CompilerMessageType.Error) continue;
                    failed = true;
                    Errors.Add(message.message); // already "file(line,col): error ID: text"
                }
                if (failed) FailedAssemblies.Add(Path.GetFileName(assembly));
            };
            CompilationPipeline.compilationFinished += _ =>
            {
                Report report = new()
                {
                    startedUnixMs = _started,
                    finishedUnixMs = Now(),
                    failed = Errors.Count > 0,
                    failedAssemblies = FailedAssemblies.ToArray(),
                    errors = Errors.ToArray(),
                };
                Directory.CreateDirectory("artifacts/ui");
                File.WriteAllText("artifacts/ui/compile-result.json", JsonUtility.ToJson(report, true));
            };
        }

        private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
