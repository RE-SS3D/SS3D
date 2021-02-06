using UnityEditor;

namespace SS3D.Editor
{
    [InitializeOnLoad]
    class StopPlayOnRecompile
    {
        static StopPlayOnRecompile()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += OnPlayModeChange;
        }
 
        private static void Update()
        {
            if (EditorApplication.isCompiling && EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }
 
        private static void OnPlayModeChange(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.LockReloadAssemblies();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }
    }
}