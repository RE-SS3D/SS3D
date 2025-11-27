using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Used on bodypart of entities to indicate which type of clothes can be worn on them.
    /// </summary>
    public class ClothedBodyPart : MonoBehaviour
    {
        [FormerlySerializedAs("clothType")]
        [SerializeField]
        private ClothType _clothType;

        public ClothType Type => _clothType;
    }
}
