using System;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SS3D.Attributes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AssetAudit
{
    /// <summary>
    /// Audits every scene under <see cref="AssetAuditUtilities.SceneRootPath"/>. Each scene loads as a preview scene,
    /// so the scenes open in the Editor stay as they are. Opening them with EditorSceneManager.OpenScene left the last
    /// one open as the active scene for the rest of the run.
    /// </summary>
    public class SceneTests
    {
        private Scene _previewScene;

        [TearDown]
        public void ClosePreviewScene()
        {
            if (_previewScene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(_previewScene);
            }

            _previewScene = default;
        }

        #region Tests
        /// <summary>
        /// Test to confirm that MonoBehaviours have serialized fields (marked by NotNullAttribute) initialized.
        /// The purpose of this test is to prevent NullReferenceExceptions caused by failing to initialize MonoBehaviour fields.
        /// </summary>
        [Test, TestCaseSource(nameof(AllScenes))]
        public void SpecifiedFieldsWithinSceneAreNotNull(SceneAsset scene)
        {
            bool allRelevantFieldsHaveBeenSet = true;
            BindingFlags flags = GetBindingFlags();
            StringBuilder sb = new();

            // ACT - Check each MonoBehaviour in the scene
            foreach (MonoBehaviour mono in SceneMonoBehaviours(scene))
            {
                // Missing scripts appear as null entries; SceneObjectsDoNotHaveMissingScripts covers those.
                if (mono == null)
                {
                    continue;
                }

                // Get all fields from the MonoBehaviour using reflection
                Type monoType = mono.GetType();
                FieldInfo[] objectFields = monoType.GetFields(flags);

                // Check the fields to see if they have a NotNullAttribute
                foreach (FieldInfo t in objectFields)
                {
                    NotNullAttribute attribute = Attribute.GetCustomAttribute(t, typeof(NotNullAttribute)) as NotNullAttribute;
                    if (attribute == null)
                    {
                        continue;
                    }

                    // Once we are here, we have found a MonoBehaviour field with a NotNullAttribute.
                    // We now need to test the field to see if it is a value set.
                    object fieldValue = t.GetValue(mono);

                    if (fieldValue != null && fieldValue.ToString() != "null")
                    {
                        continue;
                    }

                    // The test will fail, as the MonoBehaviour SHOULD have had some value in the required field, but DID NOT.
                    // We are delaying the assertion so that all errors are identified in the console, rather than requiring the
                    // test to be run multiple times (and only identifying a single breach each time).
                    allRelevantFieldsHaveBeenSet = false;
                    sb.Append($"-> Scene object '{mono.gameObject.name}' does not have {t.Name} field set in {monoType.Name} script.\n");
                }
            }

            // ASSERT
            Assert.IsTrue(allRelevantFieldsHaveBeenSet, sb.ToString());
        }

        /// <summary>
        /// Test to confirm that GameObjects within the tested scene are on the correct layers.
        /// The purpose of this test is to ensure layer-based collisions, raycasts, rendering etc function correctly.
        /// </summary>
        [Test, TestCaseSource(nameof(AllScenes))]
        public void SceneObjectsAreOnTheirMandatedLayers(SceneAsset scene)
        {
            StringBuilder sb = new();
            bool allRelevantMonoBehavioursAreOnTheRightLayer =
                AssetAuditUtilities.CheckMonoBehavioursForCorrectLayer(SceneMonoBehaviours(scene), ref sb);
            Assert.IsTrue(allRelevantMonoBehavioursAreOnTheRightLayer, sb.ToString());
        }

        /// <summary>
        /// Test to confirm that gameobjects within scenes do not have missing scripts.
        /// Missing scripts can occur when a script is deleted, or when script meta files are recreated.
        /// </summary>
        [Test, TestCaseSource(nameof(AllScenes))]
        public void SceneObjectsDoNotHaveMissingScripts(SceneAsset scene)
        {
            StringBuilder sb = new();
            foreach (GameObject sceneObject in SceneGameObjects(scene))
            {
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(sceneObject);
                if (missingCount > 0)
                {
                    sb.Append($"-> {missingCount} missing script(s) on '{sceneObject.name}'.\n");
                }
            }

            Assert.IsTrue(sb.Length == 0, sb.ToString());
        }
        #endregion

        #region Helper functions
        private BindingFlags GetBindingFlags()
        {
            BindingFlags flags = BindingFlags.Public |
                                 BindingFlags.Instance |
                                 BindingFlags.NonPublic;
            return flags;
        }

        private static SceneAsset[] AllScenes()
        {
            return AssetAuditUtilities.AllScenes();
        }

        /// <summary>
        /// Loads the scene as a preview scene (closed in <see cref="ClosePreviewScene"/>) and returns its root objects.
        /// </summary>
        private GameObject[] OpenSceneRoots(SceneAsset scene)
        {
            _previewScene = EditorSceneManager.OpenPreviewScene(AssetDatabase.GetAssetPath(scene));
            return _previewScene.GetRootGameObjects();
        }

        /// <summary>
        /// Every MonoBehaviour in the scene, inactive ones included.
        /// </summary>
        private MonoBehaviour[] SceneMonoBehaviours(SceneAsset scene)
        {
            return OpenSceneRoots(scene)
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .ToArray();
        }

        /// <summary>
        /// Every GameObject in the scene, inactive ones included.
        /// </summary>
        private GameObject[] SceneGameObjects(SceneAsset scene)
        {
            return OpenSceneRoots(scene)
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject)
                .ToArray();
        }
        #endregion
    }
}
