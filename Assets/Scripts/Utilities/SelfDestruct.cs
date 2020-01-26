using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// Helper MonoBehaviour to destroy a GameObject after a set timer.
    /// </summary>
    public class SelfDestruct : MonoBehaviour
    {
        [SerializeField] private float time = 0f;

        private void Start()
        {
            Destroy(gameObject, time);
        }
    }
}
