using SS3D.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AssetAuditUtilities
{
    public const string PrefabRootPath = "Assets/Content";
    public const string SceneRootPath = "Assets/Scenes";


    public static GameObject[] AllPrefabs()
    {
        // Find all the prefabs in the project hierarchy (i.e. NOT in a scene)
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { PrefabRootPath });

        // Create our array of prefabs
        GameObject[] prefabs = new GameObject[guids.Length];

        // Populate the array
        for (int i = 0; i < guids.Length; i++)
        {
            prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        return prefabs;
    }

    public static SceneAsset[] AllScenes()
    {
        // Find all the prefabs in the project hierarchy (i.e. NOT in a scene)
        string[] guids = AssetDatabase.FindAssets("t:scene", new[] { SceneRootPath });

        // Create our array of prefabs
        SceneAsset[] scenes = new SceneAsset[guids.Length];

        // Populate the array
        for (int i = 0; i < guids.Length; i++)
        {
            scenes[i] = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        return scenes;
    }



    public static bool CheckMonoBehavioursForCorrectLayer(MonoBehaviour[] behaviours, ref StringBuilder sb)
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

    public static bool CheckGameObjectForMissingScripts(GameObject gameobject, ref StringBuilder sb)
    {
        bool allScriptsExist = true;
        MonoBehaviour[] monobehaviours = gameobject.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour mono in monobehaviours)
        {
            if (mono == null)
            {
                allScriptsExist = false;
                sb.Append($"-> Missing script on '{gameobject.name}'.\n");
                continue;
            }
        }
        return allScriptsExist;
    }
}
