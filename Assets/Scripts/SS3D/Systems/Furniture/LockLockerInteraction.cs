using JetBrains.Annotations;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public sealed class LockLockerInteraction : Interaction
    {
        private readonly IDPermission _permissionToUnlock;

        private readonly Locker _locker;

        public LockLockerInteraction(Locker locker, IDPermission permission)
        {
            _locker = locker;
            _permissionToUnlock = permission;
        }

        [NotNull]
        public override string GetName(InteractionEvent interactionEvent) => "Lock Locker";

        public override Sprite GetIcon(InteractionEvent interactionEvent) => IconOverride != null ? IconOverride : Assets.Get(InteractionIcons.Open);

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            return !_locker.Locked;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;

            if (source is not IGameObjectProvider sourceGameObjectProvider)
            {
                return false;
            }

            Hands hands = sourceGameObjectProvider.ProvidedGameObject.GetComponentInParent<Hands>();

            if (hands == null)
            {
                return false;
            }

            if (hands.Inventory.HasPermission(_permissionToUnlock))
            {
                Punpun.Information(this, "Locker has been locked!");
                _locker.Locked = true;
            }
            else
            {
                Punpun.Information(this, "No permission to lock Locker!");
                return false;
            }

            return true;
        }
    }
}