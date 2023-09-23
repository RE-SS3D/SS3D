using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SS3D.Attributes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetAudit
{
    public class SceneTests
    {
        /// <summary>
        /// Test to confirm that MonoBehaviours have serialized fields (marked by NotNullAttribute) initialized.
        /// The purpose of this test is to prevent NullReferenceExceptions caused by failing to initialize MonoBehaviour fields.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllScenes))]
        public void SpecifiedFieldsWithinSceneAreNotNull(SceneAsset scene)
        {
            // Load all MonoBehaviours in the desired scene
            EditorSceneManager.OpenScene(FullScenePathAndName(scene.name));
            MonoBehaviour[] allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();

            bool allRelevantFieldsHaveBeenSet = true;
            BindingFlags flags = GetBindingFlags();
            StringBuilder sb = new();

            // ACT - Check each MonoBehaviour in the scene
            foreach (MonoBehaviour mono in allMonoBehaviours)
            {
                // Get all fields from the MonoBehaviour using reflection
                Type monoType = mono.GetType();
                FieldInfo[] objectFields = monoType.GetFields(flags);

                // Check the fields to see if they have a NotNullAttribute
                foreach (FieldInfo t in objectFields)
                {
                    NotNullAttribute attribute = (NotNullAttribute)Attribute.GetCustomAttribute(t, typeof(NotNullAttribute));
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
                    #pragma warning disable RCS1197
                    sb.Append($"-> Scene object '{mono.gameObject.name}' does not have {t.Name} field set in {monoType.Name} script.\n");
                    #pragma warning restore RCS1197
                }
            }

            // ASSERT
            Assert.IsTrue(allRelevantFieldsHaveBeenSet, sb.ToString());
        }

        /// <summary>
        /// Test to confirm that GameObjects within the tested scene are on the correct layers.
        /// The purpose of this test is to ensure layer-based collisions, raycasts, rendering etc function correctly.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllScenes))]
        public void SceneObjectsAreOnTheirMandatedLayers(SceneAsset scene)
        {
            StringBuilder sb = new();
            EditorSceneManager.OpenScene(FullScenePathAndName(scene.name));
            MonoBehaviour[] sceneMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
            bool allRelevantMonoBehavioursAreOnTheRightLayer = AssetAuditUtilities.CheckMonoBehavioursForCorrectLayer(sceneMonoBehaviours, ref sb);
            Assert.IsTrue(allRelevantMonoBehavioursAreOnTheRightLayer, sb.ToString());
        }

        /// <summary>
        /// Test to confirm that gameobjects within scenes do not have missing scripts.
        /// Missing scripts can occur when a script is deleted, or when script meta files are recreated.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllScenes))]
        public void SceneObjectsDoNotHaveMissingScripts(SceneAsset scene)
        {
            StringBuilder sb = new();
            EditorSceneManager.OpenScene(FullScenePathAndName(scene.name));
            bool allScriptsExist = true;
            GameObject[] sceneGameObjects = Object.FindObjectsOfType<GameObject>();
            foreach (GameObject sceneObject in sceneGameObjects)
            {
                allScriptsExist = allScriptsExist && AssetAuditUtilities.CheckGameObjectForMissingScripts(sceneObject, ref sb);
            }
            Assert.IsTrue(allScriptsExist, sb.ToString());
        }

        private BindingFlags GetBindingFlags()
        {
            const BindingFlags flags = BindingFlags.Public |
                                 BindingFlags.Instance |
                                 BindingFlags.NonPublic;
            return flags;
        }

        private static SceneAsset[] AllScenes()
        {
            return AssetAuditUtilities.AllScenes();
        }

        private static string FullScenePathAndName(string sceneName)
        {
            return $"{AssetAuditUtilities.SceneRootPath}/{sceneName}.unity";
        }
    }
}
