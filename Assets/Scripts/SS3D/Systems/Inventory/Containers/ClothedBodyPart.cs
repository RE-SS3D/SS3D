using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Used on bodypart of entities to indicate which type of clothes can be worn on them.
    /// </summary>
    public class ClothedBodyPart : MonoBehaviour
    {
        /// <summary>
        /// The type of clothes that can be worn on this body part.
        /// </summary>
        [SerializeField]
        private ClothType _clothType;

        /// <summary>
        /// The type of clothes that can be worn on this body part.
        /// </summary>
        public ClothType Type => _clothType;
    }
}