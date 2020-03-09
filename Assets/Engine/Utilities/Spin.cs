using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// Helper class to rotate an object around the vertical axis.
    /// </summary>
    public class Spin : MonoBehaviour
    {
        [SerializeField] private float speed = 1f;

        private void Update()
        {
            transform.Rotate(0, speed * Time.deltaTime, 0);
        }
    }
}
