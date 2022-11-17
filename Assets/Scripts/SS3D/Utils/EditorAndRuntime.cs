using UnityEditor;
using UnityEngine;

namespace SS3D.Utils
{
        /// <summary>
        /// Mini scripts for performing actions that are slightly different between in-editor and runtime.
        /// </summary>
        public static class EditorAndRuntime
        {
                public static void Destroy(GameObject gameObject)
                {
#if UNITY_EDITOR
                        Object.DestroyImmediate(gameObject);
#else
        Object.Destroy(gameObject);
#endif
                }

                public static GameObject InstantiatePrefab(GameObject gameObject)
                {
#if UNITY_EDITOR
                        return (GameObject)PrefabUtility.InstantiatePrefab(gameObject);
#else
        return GameObject.Instantiate(gameObject);
#endif
                }
                public static GameObject InstantiatePrefab(GameObject gameObject, Transform transform)
                {
#if UNITY_EDITOR
                        return (GameObject)PrefabUtility.InstantiatePrefab(gameObject, transform);
#else
        return GameObject.Instantiate(gameObject, transform);
#endif
                }
        }
}
