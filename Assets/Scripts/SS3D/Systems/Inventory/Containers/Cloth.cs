using SS3D.Systems.Inventory.Containers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Script to put on cloth, which are gameobjects going to the clothes slots.
    /// In the future, should be the folded models, and could contain a reference to the worn mesh version (and maybe torn mesh version, stuff like that...)
    /// </summary>
    public class Cloth : MonoBehaviour
    {
        [SerializeField]
        private ClothType clothType;

        public ClothType Type => clothType;
    }
}
