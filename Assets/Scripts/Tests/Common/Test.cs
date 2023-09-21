using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>
    /// Base testing class, inherited by PlayModeTest and EditModeTest.
    /// </summary>
    [TestFixture]
    public abstract class Test
    {
        /// <summary>
        /// Keep track of any objects instantiated during tests.
        /// </summary>
        public List<GameObject> instantiated;

        public virtual void SetUp()
        {
            instantiated = new List<GameObject>();
        }

        public virtual void TearDown()
        {
            // Clear up anything we instantiated.
            foreach (GameObject go in instantiated)
            {
                GameObject.DestroyImmediate(go);
            }
        }

        protected void CreateGameObject(out GameObject go)
        {
            go = new GameObject();
            instantiated.Add(go);
        }

        protected void CreateGameObject<T>(out GameObject go, out T component) where T : MonoBehaviour
        {
            CreateGameObject(out go);
            component = go.AddComponent<T>();
        }
    }
}