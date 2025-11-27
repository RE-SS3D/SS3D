using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    public interface IItemHolder
    {
        public Item ItemHeld { get; }

        public bool Empty { get; }
    }
}
