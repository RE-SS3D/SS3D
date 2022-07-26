using NUnit.Framework;
using UnityEditor.SceneManagement;
using System.Reflection;
using static DataSource;
using UnityEngine;
using System;
using UnityEditor;

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
    #endregion

    #region Test set up
    [SetUp]
    public void SetUp()
    {
        EditorSceneManager.OpenScene(SCENE_PATH);
        LoadAllPrefabs();
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
    #endregion

    #region Tests
    /// <summary>
    /// Test to confirm that designated MonoBehaviours have required serialized fields initialized.
    /// The purpose of this test is to prevent NullReferenceExceptions caused by failing to initialize MonoBehaviour fields.
    /// </summary>
    /// <param name="data">VariableDetails listing the target class and associated variable details.</param>
    [TestCaseSource(typeof(DataSource), nameof(DataSource.NullCheckData))]
    public void SerializedFieldsAreNotNull(VariableDetails data)
    {
        // Arrange
        FieldInfo field;
        BindingFlags flags = GetBindingFlags();
        var gameObjects = UnityEngine.Object.FindObjectsOfType(data.className);

        // Act #1 - Get the fields from each tested script
        foreach (string s in data.variableNames)
        {
            field = data.className.GetField(s, flags);

            // Assert #1: The tested script actually contains the fields you are testing for
            Assert.IsNotNull(field, "{0} does not appear to have a field for {1}.", data.className.ToString(), s);

            // Act #2 - Get the values of those fields from the GameObjects component
            foreach (var gameObject in gameObjects)
            {
                field = data.className.GetField(s, flags);

                // Assert #2: The GameObject component has a value set in the tested field.
                Assert.IsNotNull(field.GetValue(gameObject), "{0} did not have {1} set.", gameObject.name, s);
            }
        }
    }

    /// <summary>
    /// Test to confirm that GameObjects within the tested scene are on the correct layers.
    /// The purpose of this test is to ensure layer-based collisions, raycasts, rendering etc function correctly.
    /// </summary>
    /// <param name="data">MandatoryLayerForClass listing the target class and the layer it is required to be on.</param>
    [TestCaseSource(typeof(DataSource), nameof(DataSource.MandatoryLayerCheckData))]
    public void GameObjectsAreOnTheirMandatedLayers(MandatoryLayerForClass data)
    {
        // Arrange
        var gameObjects = UnityEngine.Object.FindObjectsOfType(data.className);

        // Act
        foreach (var gameObject in gameObjects)
        {
            MonoBehaviour mb = gameObject as MonoBehaviour;
            if (mb == null) continue;
            bool correctLayer = mb.gameObject.layer == LayerMask.NameToLayer(data.layerName);

            // Assert: The GameObject is on the correct layer.
            Assert.IsTrue(correctLayer, "{0} (of type <{1}>) is not on {2} layer.", mb.gameObject.name, data.className.Name, data.layerName);
        }
    }

    /// <summary>
    /// Test to confirm that prefabs within the project are on the correct layers.
    /// The purpose of this is to ensure that any prefabs instantiated at runtime are added to the correct layer.
    /// </summary>
    /// <param name="data">MandatoryLayerForClass listing the target class and the layer it is required to be on.</param>
    [TestCaseSource(typeof(DataSource), nameof(DataSource.MandatoryLayerCheckData))]
    public void PrefabsAreOnTheirMandatedLayers(MandatoryLayerForClass data)
    {
        // Arrange -> prefab array arranged during SetUp().

        // Act
        Type targetType = data.className;
        foreach (GameObject prefab in allPrefabs)
        {
            
            if (prefab.GetComponent(data.className) != null)
            {
                bool correctLayer = prefab.layer == LayerMask.NameToLayer(data.layerName);

                // Assert: The prefab is on the correct layer.
                Assert.IsTrue(correctLayer, "Prefab {0} (of type <{1}>) is not on {2} layer.", prefab.name, data.className.Name, data.layerName);
            }
        }
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
