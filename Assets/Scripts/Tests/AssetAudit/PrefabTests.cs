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
    public class PrefabTests
    {
        #region Tests
        /// <summary>
        /// Test to confirm that prefabs within the project are on the correct layers.
        /// The purpose of this is to ensure that any prefabs instantiated at runtime are added to the correct layer.
        /// </summary>
        [Test, TestCaseSource(nameof(AllPrefabs))]
        public void PrefabsAreOnTheirMandatedLayers(GameObject prefab)
        {
            StringBuilder sb = new();
            MonoBehaviour[] prefabMonoBehaviours = prefab.GetComponentsInChildren<MonoBehaviour>();
            bool allRelevantMonoBehavioursAreOnTheRightLayer = AssetAuditUtilities.CheckMonoBehavioursForCorrectLayer(prefabMonoBehaviours, ref sb);
            Assert.IsTrue(allRelevantMonoBehavioursAreOnTheRightLayer, sb.ToString());
        }

        /// <summary>
        /// Test to confirm that prefabs within the project do not have missing scripts.
        /// Missing scripts can occur when a script is deleted, or when script meta files are recreated.
        /// </summary>
        [Test, TestCaseSource(nameof(AllPrefabs))]
        public void PrefabsDoNotHaveMissingScripts(GameObject prefab)
        {
            StringBuilder sb = new();
            bool allScriptsExist = AssetAuditUtilities.CheckGameObjectForMissingScripts(prefab, ref sb);
            Assert.IsTrue(allScriptsExist, sb.ToString());
        }
        #endregion

        #region Helper functions
        public static GameObject[] AllPrefabs()
        {
            return AssetAuditUtilities.AllPrefabs();
        }

        #endregion
    }
}
