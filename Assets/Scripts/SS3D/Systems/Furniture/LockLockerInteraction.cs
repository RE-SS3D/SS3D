using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public sealed class LockLockerInteraction : Interaction
    {
        private IDPermission permissionToUnlock;

        public event EventHandler<bool> OnOpenStateChanged;
        Locker locker;

        public LockLockerInteraction(Locker locker, IDPermission permission)
        {
            this.locker = locker;
            this.permissionToUnlock = permission;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Lock Locker";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Assets.Get(InteractionIcons.Open);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            return !locker.Locked;
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;
            if (source is IGameObjectProvider sourceGameObjectProvider)
            {
                Hands hands = sourceGameObjectProvider.GameObject.GetComponentInParent<Hands>();
                
                if (hands != null)
                {
                    if (hands.Inventory.HasPermission(permissionToUnlock))
                    {
                        Log.Information(this, "Locker has been locked!");
                        locker.Locked = true;
                    } else
                    {
                        Log.Information(this, "No permission to lock Locker!");
                        return false;
                    }
                }

                return true;
            }
            return false;
        }
    }
}