using UnityEngine;

namespace SS3D.Utils
{
    public class DestroyIfNotDevelopmentBuild : MonoBehaviour
    {
        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                Destroy(gameObject);
            }
        }
    }
}
