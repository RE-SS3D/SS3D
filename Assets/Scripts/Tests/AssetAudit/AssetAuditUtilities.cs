using SS3D.Attributes;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetAudit
{
    public static class AssetAuditUtilities
    {
        public const string PrefabRootPath = "Assets/Content";
        public const string SceneRootPath = "Assets/Content/Scenes";
        public const string GenericRootPath = "Assets";
        private const string PrefabSearchTerm = "t:prefab";
        private const string SceneSearchTerm = "t:scene";
        private const string TileObjectSoSearchTerm = "t:TileObjectSo";
        private const string ItemObjectSoSearchTerm = "t:ItemObjectSo";

        public static GameObject[] AllPrefabs()
        {
            return GetAssets<GameObject>(PrefabSearchTerm, PrefabRootPath);
        }

        public static SceneAsset[] AllScenes()
        {
            return GetAssets<SceneAsset>(SceneSearchTerm, SceneRootPath);
        }

        public static TileObjectSo[] AllTileObjectSo()
        {
            return GetAssets<TileObjectSo>(TileObjectSoSearchTerm);
        }

        public static ItemObjectSo[] AllItemObjectSo()
        {
            return GetAssets<ItemObjectSo>(ItemObjectSoSearchTerm);
        }

        /// <summary>
        /// Generic getter method to load assets from the project hierarchy. Used to simplify calls from test scripts.
        /// </summary>
        /// <typeparam name="T">Generic type that you want returned.</typeparam>
        /// <param name="searchCriteria">Search criteria for the AssetDatabase.FindAssets() method.</param>
        /// <param name="searchPath">Root path to search in. Will default to assets.</param>
        /// <returns>An array of type T containing all instances within the desired search path.</returns>
        private static T[] GetAssets<T>(string searchCriteria, string searchPath = GenericRootPath) where T : UnityEngine.Object
        {
            // Find all the assets in the project hierarchy (i.e. NOT in a scene)
            string[] guids = AssetDatabase.FindAssets(searchCriteria, new string[] { searchPath });

            // Create our array of assets
            T[] returnValues = new T[guids.Length];

            // Populate the array
            for (int i = 0; i < guids.Length; i++)
            {
                returnValues[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            return returnValues;
        }

        public static bool CheckMonoBehavioursForCorrectLayer(MonoBehaviour[] behaviours, ref StringBuilder sb)
        {
            bool allRelevantMonoBehavioursAreOnTheRightLayer = true;
            foreach (MonoBehaviour mono in behaviours)
            {
                Type monoType = mono.GetType();
                RequiredLayerAttribute attribute = (RequiredLayerAttribute)Attribute.GetCustomAttribute(monoType, typeof(RequiredLayerAttribute));
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
                #pragma warning disable RCS1197
                sb.Append($"-> {monoType.Name} script requires object '{gameObject.name}' to be on {attribute.Layer} layer, but it was on {LayerMask.LayerToName(gameObject.layer)} layer.\n");
                #pragma warning restore RCS1197
            }

            return allRelevantMonoBehavioursAreOnTheRightLayer;
        }

        public static bool CheckGameObjectForMissingScripts(GameObject gameobject, ref StringBuilder sb)
        {
            bool allScriptsExist = true;
            MonoBehaviour[] monobehaviours = gameobject.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour mono in monobehaviours)
            {
                if (mono != null)
                {
                    continue;
                }

                allScriptsExist = false;
                #pragma warning disable RCS1197
                sb.Append($"-> Missing script on '{gameobject.name}'.\n");
                #pragma warning restore RCS1197
            }

            return allScriptsExist;
        }
    }
}
