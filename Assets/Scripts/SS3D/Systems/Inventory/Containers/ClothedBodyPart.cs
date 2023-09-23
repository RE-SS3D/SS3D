using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Used on bodypart of entities to indicate which type of clothes can be worn on them.
    /// </summary>
    public class ClothedBodyPart : MonoBehaviour
    {
        [SerializeField]
        private ClothType clothType;

        public ClothType Type => clothType;
    }
}