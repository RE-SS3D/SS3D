using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Simple class to mark that a container should be treated as the kind that can contain clothes only.
    /// </summary>
    public class ClothContainer : MonoBehaviour
    {
        [SerializeField]
        private ClothType _clothType;

        public ClothType ClothType => _clothType;
    }
}