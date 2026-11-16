#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Substances.Editor
{
    /// <summary>Strip empty legacy <see cref="SubstanceContainer"/> from Human.prefab (tier B).</summary>
    public static class HumanSubstanceContainerStrip
    {
        private const string HumanPrefabPath =
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";

        public static bool Strip()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HumanPrefabPath);
            try
            {
                SubstanceContainer[] containers = root.GetComponentsInChildren<SubstanceContainer>(true);
                if (containers.Length == 0)
                {
                    return false;
                }

                for (int i = 0; i < containers.Length; i++)
                {
                    Object.DestroyImmediate(containers[i], true);
                }

                SubstanceContainerExaminable[] examinables =
                    root.GetComponentsInChildren<SubstanceContainerExaminable>(true);
                for (int i = 0; i < examinables.Length; i++)
                {
                    Object.DestroyImmediate(examinables[i], true);
                }

                PrefabUtility.SaveAsPrefabAsset(root, HumanPrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
