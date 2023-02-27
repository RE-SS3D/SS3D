using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using SS3D.Attributes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    public class SceneTests
    {
        #region Class variables
        /// <summary>
        /// The name and path of the Scene that this script is testing.
        /// </summary>
        private const string SCENE_PATH = "Assets/Scenes/Lobby.unity";

        /// <summary>
        /// All of the prefabs in the project.
        /// </summary>
        private GameObject[] _allPrefabs;

        /// <summary>
        /// All of the MonoBehaviours in the current scene.
        /// </summary>
        private MonoBehaviour[] _allMonoBehaviours;

        #endregion

        #region Test set up
        [OneTimeSetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene(SCENE_PATH);
            LoadAllPrefabs();
            LoadAllMonoBehaviours();
        }

        private void LoadAllPrefabs()
        {
            // Find all the prefabs in the project hierarchy (i.e. NOT in a scene)
            string[] guids = AssetDatabase.FindAssets("t:prefab");

            // Create our array of prefabs
            _allPrefabs = new GameObject[guids.Length];

            // Populate the array
            for (int i = 0; i <guids.Length; i++)
            {
                _allPrefabs[i] = AssetDatabase.LoadAssetAtPath <GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }
        }

        private void LoadAllMonoBehaviours()
        {
            // Find all the MonoBehaviours in the currently open scene
            _allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
        }
        #endregion

        #region Tests

        /// <summary>
        /// Test to confirm that MonoBehaviours have serialized fields (marked by NotNullAttribute) initialized.
        /// The purpose of this test is to prevent NullReferenceExceptions caused by failing to initialize MonoBehaviour fields.
        /// </summary>
        [Test, TestCaseSource(nameof(AllScenes))]
        public void SpecifiedFieldsWithinSceneAreNotNull(SceneAsset scene)
        {

            // ARRANGE
            EditorSceneManager.OpenScene(scene.name);

            bool allRelevantFieldsHaveBeenSet = true;
            BindingFlags flags = GetBindingFlags();
            StringBuilder sb = new();

            // ACT - Check each MonoBehaviour in the scene
            foreach (MonoBehaviour mono in _allMonoBehaviours)
            {
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
        [Test]
        public void SceneObjectsAreOnTheirMandatedLayers()
        {
            // ARRANGE
            StringBuilder sb = new();

            // ACT
            bool allRelevantMonoBehavioursAreOnTheRightLayer = AssetAuditUtilities.CheckMonoBehavioursForCorrectLayer(_allMonoBehaviours, ref sb);

            // ASSERT
            Assert.IsTrue(allRelevantMonoBehavioursAreOnTheRightLayer, sb.ToString());
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
        #endregion
    }
}
