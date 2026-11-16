using FishNet.Connection;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    internal static class MachineInterfaceOperatorResolver
    {
        public static bool TryResolveInventory(NetworkConnection conn, out HumanInventory inventory)
        {
            inventory = null;
            if (conn == null || !conn.IsValid || conn.FirstObject == null)
            {
                return false;
            }

            Entity entity = conn.FirstObject.GetComponent<Entity>();
            inventory = entity != null
                ? entity.GetComponent<HumanInventory>()
                : conn.FirstObject.GetComponent<HumanInventory>();

            return inventory != null;
        }
    }
}
