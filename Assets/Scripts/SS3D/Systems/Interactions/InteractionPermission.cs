using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using IDPermission = global::SS3D.Systems.IDPermission;

namespace SS3D.Systems.Interactions
{
    public static class InteractionPermission
    {
        public static bool HasPermission(InteractionEvent interactionEvent, IDPermission permission)
        {
            if (permission == null)
            {
                return true;
            }

            if (interactionEvent.Source is not IGameObjectProvider provider)
            {
                return false;
            }

            Hands hands = provider.GameObject.GetComponentInParent<Hands>();

            return hands?.Inventory.HasPermission(permission) ?? false;
        }
    }
}
