using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Containers;
using System;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public sealed class UnlockLockerInteraction : IInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;
        private readonly IDPermission _permissionToUnlock;
        private readonly Locker _locker;

        public event EventHandler<bool> OnOpenStateChanged;

        public UnlockLockerInteraction(Locker locker, IDPermission permission)
        {
            _locker = locker;
            _permissionToUnlock = permission;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Unlock Locker";
        }

        public string GetGenericName() => throw new NotImplementedException();

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Open);
        }

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

            if (source is IGameObjectProvider sourceGameObjectProvider)
            {
                Hands hands = sourceGameObjectProvider.GameObject.GetComponentInParent<Hands>();

                if (hands != null)
                {
                    if (hands.Inventory.HasPermission(_permissionToUnlock))
                    {
                        Log.Information(this, "Locker has been unlocked!");
                        _locker.IsLocked = false;
                    }
                    else
                    {
                        Log.Information(this, "No permission to unlock Locker!");
                    }
                }

                return true;
            }

            return false;
        }
    }
}