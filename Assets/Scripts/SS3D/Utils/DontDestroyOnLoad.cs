using UnityEngine;

namespace SS3D.Utils
{
    /// <summary>
    /// Makes objects persist throughout the scene. Important: Has to be in the root object as it preserves all children
    /// </summary>
    public sealed class DontDestroyOnLoad : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
