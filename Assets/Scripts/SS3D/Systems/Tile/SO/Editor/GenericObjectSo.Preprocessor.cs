#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SS3D.Systems.Tile.Editor
{
    public sealed class GenericObjectSoPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            GenericObjectSo.CreateIcons();
        }
    }
}
#endif