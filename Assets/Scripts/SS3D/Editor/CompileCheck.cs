#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Batchmode compile gate for <c>Tools/check_compile.sh</c>.
    /// Waits for script compilation, then exits 0/1. Does not run tests or build players.
    /// </summary>
    public static class CompileCheck
    {
        /// <summary>
        /// Batchmode: <c>-executeMethod SS3D.Editor.CompileCheck.RunBatch</c> (no <c>-quit</c>;
        /// this method calls <see cref="EditorApplication.Exit"/>).
        /// </summary>
        public static void RunBatch()
        {
            void Tick()
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    return;
                }

                EditorApplication.update -= Tick;

                if (EditorUtility.scriptCompilationFailed)
                {
                    Debug.LogError("CompileCheck: FAILED — EditorUtility.scriptCompilationFailed");
                    EditorApplication.Exit(1);
                    return;
                }

                Debug.Log("CompileCheck: OK");
                EditorApplication.Exit(0);
            }

            EditorApplication.update += Tick;
            CompilationPipeline.RequestScriptCompilation();
        }
    }
}
#endif
