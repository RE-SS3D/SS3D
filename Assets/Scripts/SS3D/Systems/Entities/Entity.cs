using FishNet.Object;
using SS3D.Core.Systems.Inventory;
using UnityEngine;

namespace SS3D.Core.Systems.Entities
{
    /// <summary>
    /// Base class for all things that can be controlled by a player
    /// </summary>
    public class Entity : NetworkBehaviour
    {
        [SerializeField] private EntityInventory _inventory;

        public EntityInventory Inventory => _inventory;
        
    }
}