using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// Helper class to rotate an object around the vertical axis.
    /// </summary>
    public class Spin : MonoBehaviour
    {
        [SerializeField] private float speed;

        private void Update()
        {
            Vector3 current = transform.eulerAngles;
            transform.eulerAngles = new Vector3(current.x, current.y + speed * Time.deltaTime, current.z);
        }
    }
}
