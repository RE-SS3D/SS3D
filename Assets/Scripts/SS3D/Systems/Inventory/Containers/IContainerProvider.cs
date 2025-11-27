using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    public interface IContainerProvider
    {
        public AttachedContainer Container { get; }
    }
}
