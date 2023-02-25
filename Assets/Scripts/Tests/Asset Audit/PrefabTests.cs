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
    public class PrefabTests
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
        [Test]
        public void SpecifiedFieldsWithinSceneAreNotNull()
        {

            // ARRANGE
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
        public void SpecifiedMonoBehavioursWithinSceneAreOnTheirMandatedLayers()
        {
            // ARRANGE
            StringBuilder sb = new();

            // ACT
            bool allRelevantMonoBehavioursAreOnTheRightLayer = CheckMonoBehavioursForCorrectLayer(_allMonoBehaviours, ref sb);

            // ASSERT
            Assert.IsTrue(allRelevantMonoBehavioursAreOnTheRightLayer, sb.ToString());
        }

        /// <summary>
        /// Test to confirm that prefabs within the project are on the correct layers.
        /// The purpose of this is to ensure that any prefabs instantiated at runtime are added to the correct layer.
        /// </summary>
        [Test]
        public void SpecifiedMonoBehavioursWithinPrefabsAreOnTheirMandatedLayers()
        {

            // ARRANGE
            bool allRelevantMonoBehavioursAreOnTheRightLayer = true;
            StringBuilder sb = new();

            // ACT
            foreach (GameObject prefab in _allPrefabs)
            {
                MonoBehaviour[] prefabMonoBehaviours = prefab.GetComponentsInChildren<MonoBehaviour>();
                allRelevantMonoBehavioursAreOnTheRightLayer =
                    allRelevantMonoBehavioursAreOnTheRightLayer && CheckMonoBehavioursForCorrectLayer(prefabMonoBehaviours, ref sb);
            }

            // ASSERT
            Assert.IsTrue(allRelevantMonoBehavioursAreOnTheRightLayer, sb.ToString());
        }

        /// <summary>
        /// Test to confirm that prefabs within the project do not have missing scripts.
        /// Missing scripts can occur when a script is deleted, or when script meta files are recreated.
        /// </summary>
        [Test]
        public void PrefabsDoNotHaveMissingScripts()
        {
            bool allScriptsExist = true;
            StringBuilder sb = new();

            foreach (GameObject prefab in _allPrefabs)
            {
                MonoBehaviour[] prefabMonoBehaviours = prefab.GetComponentsInChildren<MonoBehaviour>();
                allScriptsExist = allScriptsExist && CheckPrefabForMissingScripts(prefabMonoBehaviours, ref sb, prefab.name);
            }

            Assert.IsTrue(allScriptsExist, sb.ToString());
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

        private static bool CheckPrefabForMissingScripts(MonoBehaviour[] behaviours, ref StringBuilder sb, string prefabName)
        {
            bool allScriptsExist = true;
            foreach (MonoBehaviour mono in behaviours)
            {
                if (mono == null)
                {
                    allScriptsExist = false;
                    sb.Append($"-> Missing script on '{prefabName}'.\n");
                    continue;
                }
            }
            return allScriptsExist;
        }

        private static bool CheckMonoBehavioursForCorrectLayer(MonoBehaviour[] behaviours, ref StringBuilder sb)
        {
            bool allRelevantMonoBehavioursAreOnTheRightLayer = true;
            foreach (MonoBehaviour mono in behaviours)
            {
                Type monoType = mono.GetType();
                RequiredLayerAttribute attribute = Attribute.GetCustomAttribute(monoType, typeof(RequiredLayerAttribute)) as RequiredLayerAttribute;
                if (attribute == null)
                {
                    continue;
                }
                // Once we are here, we have found a MonoBehaviour with a RequiredLayerAttribute.
                // We now need to test the GameObject to see if it is on the layer that is mandated.

                if (mono.gameObject.layer == LayerMask.NameToLayer(attribute.Layer))
                {
                    continue;
                }

                // The test will fail, as the GameObject SHOULD have had been on a specific layer, but WAS NOT.
                // We are delaying the assertion so that all errors are identified in the console, rather than requiring the
                // test to be run multiple times (and only identifying a single breach each time).
                allRelevantMonoBehavioursAreOnTheRightLayer = false;
                GameObject gameObject = mono.gameObject;
                sb.Append($"-> {monoType.Name} script requires object '{gameObject.name}' to be on {attribute.Layer} layer, but it was on {LayerMask.LayerToName(gameObject.layer)} layer.\n");
            }
            return allRelevantMonoBehavioursAreOnTheRightLayer;
        }

        #endregion
    }
}
