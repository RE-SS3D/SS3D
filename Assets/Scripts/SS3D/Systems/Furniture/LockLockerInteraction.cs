using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Furniture;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Traits;
using System;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public sealed class LockLockerInteraction : IInteraction
    {
        private readonly IDPermission _permissionToUnlock;
        private readonly Locker _locker;

        public LockLockerInteraction(Locker locker, IDPermission permission)
        {
            _locker = locker;
            _permissionToUnlock = permission;
        }

        public InteractionType InteractionType => InteractionType.None;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        public string GetName(InteractionEvent interactionEvent) => "Lock Locker";

        public string GetGenericName() => "Lock Locker";

        public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Open;

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            if (!_locker.Lockable)
            {
                return false;
            }

            return !_locker.IsLocked && !_locker.IsOpen;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;

            if (source is not IGameObjectProvider sourceGameObjectProvider)
            {
                return false;
            }

            if (sourceGameObjectProvider.GameObject.GetComponentInParent<IIDPermissionProvider>().HasPermission(_permissionToUnlock))
            {
                Log.Information(this, "Locker has been locked!");
                _locker.IsLocked = true;
            }
            else
            {
                Log.Information(this, "No permission to lock Locker!");
                return false;
            }

            return true;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
