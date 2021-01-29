using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Inventory
{
    /// <summary>
    /// Creates a container and assigns it to an attached container (thanks unity)
    /// </summary>
    public class AttachedContainerGenerator : MonoBehaviour
    {
        public AttachedContainer AttachedContainer;
        public Vector2Int Size;

        public void Start()
        {
            Assert.IsNotNull(AttachedContainer);
            
            AttachedContainer.Container = new Container
            {
                Size = Size
            };
        }
    }
}