using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Furniture;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.UI;
using SS3D.Traits;
using System;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public sealed class UnlockLockerInteraction : IInteraction
    {
        public event EventHandler<bool> OnOpenStateChanged;

        private readonly IDPermission _permissionToUnlock;

        private readonly Locker _locker;

        public UnlockLockerInteraction(Locker locker, IDPermission permission)
        {
            _locker = locker;
            _permissionToUnlock = permission;
        }

        public InteractionType InteractionType { get; }

        public IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        public string GetName(InteractionEvent interactionEvent) => "Unlock Locker";

        public string GetGenericName() => "Unlock Locker";

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

            return _locker.IsLocked && !_locker.IsOpen;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;

            if (source is not IGameObjectProvider sourceGameObjectProvider)
            {
                return false;
            }

            IIDPermissionProvider idPermission = sourceGameObjectProvider.GameObject.GetComponentInParent<IIDPermissionProvider>();

            if (idPermission.HasPermission(_permissionToUnlock))
            {
                Log.Information(this, "Locker has been unlocked!");
                _locker.IsLocked = false;
            }
            else
            {
                Log.Information(this, "No permission to unlock Locker!");
            }

            return true;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference) => false;

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
