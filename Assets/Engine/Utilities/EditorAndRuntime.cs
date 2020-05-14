using System.Collections;
using UnityEngine;
using UnityEditor;

/**
 * Mini scripts for performing actions that are slightly different between in-editor and runtime.
 */
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
        return GameObject.Instantiate(gameObject);
    }
    public static GameObject InstantiatePrefab(GameObject gameObject, Transform transform)
    {
        return GameObject.Instantiate(gameObject, transform);
    }
}
