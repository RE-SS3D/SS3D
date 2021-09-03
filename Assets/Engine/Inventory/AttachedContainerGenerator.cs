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
        public ContainerDescriptor containerDescriptor;

        public void initialize(ContainerDescriptor containerDescriptor)
        {
            this.containerDescriptor = containerDescriptor;
        }

        public void Start()
        {
            Assert.IsNotNull(containerDescriptor.attachedContainer);
            
            containerDescriptor.attachedContainer.Container = new Container
            {
                Size = containerDescriptor.Size
            };

            if (containerDescriptor.StartFilter != null)
                containerDescriptor.attachedContainer.Container.Filters.Add(containerDescriptor.StartFilter);
        }
    }
}