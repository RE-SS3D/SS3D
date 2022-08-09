using NUnit.Framework;
using UnityEditor.SceneManagement;
using System.Reflection;
using UnityEngine;
using System;
using UnityEditor;
using System.Text;

public class LobbyTests
{
    #region Class variables
    /// <summary>
    /// The name and path of the Scene that this script is testing.
    /// </summary>
    private const string SCENE_PATH = "Assets/Scenes/Lobby.unity";

    /// <summary>
    /// All of the prefabs in the project.
    /// </summary>
    private GameObject[] allPrefabs;

    /// <summary>
    /// All of the MonoBehaviours in the current scene.
    /// </summary>
    private MonoBehaviour[] allMonoBehaviours;

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
        allPrefabs = new GameObject[guids.Length];

        // Populate the array
        for (int i = 0; i <guids.Length; i++)
        {
            allPrefabs[i] = AssetDatabase.LoadAssetAtPath <GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }
    }

    private void LoadAllMonoBehaviours()
    {
        // Find all the Monobehaviours in the currently open scene
        allMonoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>();
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
        StringBuilder sb = new StringBuilder();

        // ACT - Check each MonoBehaviour in the scene
        foreach (MonoBehaviour mono in allMonoBehaviours)
        {
            // Get all fields from the MonoBehaviour using reflection
            Type monoType = mono.GetType();
            FieldInfo[] objectFields = monoType.GetFields(flags);

            // Check the fields to see if they have a NotNullAttribute
            for (int i = 0; i < objectFields.Length; i++)
            {
                NotNullAttribute attribute = Attribute.GetCustomAttribute(objectFields[i], typeof(NotNullAttribute)) as NotNullAttribute;
                if (attribute != null)
                {
                    // Once we are here, we have found a MonoBehaviour field with a NotNullAttribute.
                    // We now need to test the field to see if it is a value set.
                    var fieldValue = objectFields[i].GetValue(mono);

                    if (fieldValue == null || fieldValue.ToString() == "null")
                    {
                        // The test will fail, as the MonoBehaviour SHOULD have had some value in the required field, but DID NOT.
                        // We are delaying the assertion so that all errors are identified in the console, rather than requiring the
                        // test to be run multiple times (and only identifying a single breach each time).
                        allRelevantFieldsHaveBeenSet = false;
                        sb.Append($"-> Scene object '{mono.gameObject.name}' does not have {objectFields[i].Name} field set in {monoType.Name} script.\n");
                    }
                }
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
        bool allRelevantMonoBehavioursAreOnTheRightLayer = true;
        StringBuilder sb = new StringBuilder();

        // ACT - Check each MonoBehaviour in the scene
        foreach (MonoBehaviour mono in allMonoBehaviours)
        {
            Type monoType = mono.GetType();
            RequiredLayerAttribute attribute = Attribute.GetCustomAttribute(monoType, typeof(RequiredLayerAttribute)) as RequiredLayerAttribute;
            if (attribute != null)
            {
                // Once we are here, we have found a MonoBehaviour with a RequiredLayerAttribute.
                // We now need to test the GameObject to see if it is on the layer that is mandated.

                if (mono.gameObject.layer != LayerMask.NameToLayer(attribute.layer))
                {
                    // The test will fail, as the GameObject SHOULD have had been on a specific layer, but WAS NOT.
                    // We are delaying the assertion so that all errors are identified in the console, rather than requiring the
                    // test to be run multiple times (and only identifying a single breach each time).
                    allRelevantMonoBehavioursAreOnTheRightLayer = false;
                    sb.Append($"-> {monoType.Name} script requires scene object '{mono.gameObject.name}' to be on {attribute.layer} layer, but it was on {LayerMask.LayerToName(mono.gameObject.layer)} layer.\n");
                }
            }
        }

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
        StringBuilder sb = new StringBuilder();
        MonoBehaviour[] prefabMonoBehaviours;

        // ACT - Check each MonoBehaviour in the scene

        foreach (GameObject prefab in allPrefabs)
        {
            prefabMonoBehaviours = prefab.GetComponentsInChildren<MonoBehaviour>();

            foreach (MonoBehaviour mono in prefabMonoBehaviours)
            {
                Type monoType = mono.GetType();
                RequiredLayerAttribute attribute = Attribute.GetCustomAttribute(monoType, typeof(RequiredLayerAttribute)) as RequiredLayerAttribute;
                if (attribute != null)
                {
                    // Once we are here, we have found a MonoBehaviour with a RequiredLayerAttribute.
                    // We now need to test the GameObject to see if it is on the layer that is mandated.

                    if (mono.gameObject.layer != LayerMask.NameToLayer(attribute.layer))
                    {
                        // The test will fail, as the GameObject SHOULD have had been on a specific layer, but WAS NOT.
                        // We are delaying the assertion so that all errors are identified in the console, rather than requiring the
                        // test to be run multiple times (and only identifying a single breach each time).
                        allRelevantMonoBehavioursAreOnTheRightLayer = false;
                        sb.Append($"-> {monoType.Name} script requires object '{mono.gameObject.name}' (in prefab {prefab.name}) to be on {attribute.layer} layer, but it was on {LayerMask.LayerToName(mono.gameObject.layer)} layer.\n");
                    }
                }
            }
        }

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
    #endregion
}
