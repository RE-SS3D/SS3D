#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Substances.Editor
{
    /// <summary>Domain aggregator for substances content + registry generation (tier B).</summary>
    public static class SubstancesContentPrefabRecipes
    {
        [MenuItem("SS3D/Substances/Run Content Prefab Recipes")]
        public static void RunAllMenu()
        {
            ReagentRegistryGenerator.CreateCoreRegistry();
            int content = SubstancesContentPrefabSetup.SetupAll();
            bool hub = AssignRegistryToHub();
            bool humanStripped = HumanSubstanceContainerStrip.Strip();

            EditorUtility.DisplayDialog(
                "Substances Content Prefab Recipes",
                $"Registry generated.\n" +
                $"Content prefabs updated: {content}.\n" +
                $"Hub registry assigned: {(hub ? "yes" : "no / already set")}.\n" +
                $"Human SubstanceContainer stripped: {(humanStripped ? "yes" : "already clean")}.",
                "OK");
        }

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Substances.Editor.SubstancesContentPrefabRecipes.RunAllBatch</c></summary>
        public static void RunAllBatch()
        {
            ReagentRegistryGenerator.CreateCoreRegistry();
            int content = SubstancesContentPrefabSetup.SetupAll();
            bool hub = AssignRegistryToHub();
            bool humanStripped = HumanSubstanceContainerStrip.Strip();
            Debug.Log(
                $"[SubstancesContentPrefabRecipes] content={content}; hub={hub}; humanStrip={humanStripped}");

            if (UnityEngine.Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static bool AssignRegistryToHub()
        {
            const string hubPath = "Assets/Resources/NetworkSystemsHub.prefab";
            const string registryPath = "Assets/Content/Systems/Substances/CoreReagentRegistry.asset";
            ReagentRegistry registry = AssetDatabase.LoadAssetAtPath<ReagentRegistry>(registryPath);
            if (registry == null)
            {
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(hubPath);
            try
            {
                SubstancesSubSystem subsystem = root.GetComponentInChildren<SubstancesSubSystem>(true);
                if (subsystem == null)
                {
                    return false;
                }

                var serialized = new SerializedObject(subsystem);
                SerializedProperty registryProp = serialized.FindProperty("_registry");
                if (registryProp.objectReferenceValue == registry)
                {
                    return false;
                }

                registryProp.objectReferenceValue = registry;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, hubPath);
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
