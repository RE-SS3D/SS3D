using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using IDPermission = global::SS3D.Systems.IDPermission;

namespace SS3D.Systems.Interactions
{
    public static class InteractionPermission
    {
        public static bool HasPermission(InteractionEvent interactionEvent, AccessMask requiredAccess)
        {
            if (requiredAccess.IsNone)
            {
                return true;
            }

            if (!TryGetInventory(interactionEvent, out HumanInventory inventory))
            {
                return false;
            }

            if (!SubSystems.TryGet(out IdAccessSubSystem idAccess))
            {
                return false;
            }

            AccessCheckResult result = idAccess.CheckAccess(inventory, requiredAccess, device: null);
            return result.Passed;
        }

        public static bool HasPermission(InteractionEvent interactionEvent, IDPermission permission)
        {
            if (permission == null)
            {
                return true;
            }

            return HasPermission(interactionEvent, IdAccessPermissionMapper.FromLegacyPermission(permission));
        }

        private static bool TryGetInventory(InteractionEvent interactionEvent, out HumanInventory inventory)
        {
            inventory = null;

            if (interactionEvent.Source is not IGameObjectProvider provider)
            {
                return false;
            }

            Hands hands = provider.GameObject.GetComponentInParent<Hands>();
            inventory = hands?.Inventory;
            return inventory != null;
        }
    }
}
