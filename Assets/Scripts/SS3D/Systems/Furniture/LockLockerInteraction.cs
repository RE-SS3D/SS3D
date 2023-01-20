using SS3D.Systems.Furniture;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using UnityEngine;
using SS3D.Systems.GameModes.Events;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Storage.Containers;
using SS3D.Systems.Traits.TraitCategories;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Honks a horn. Honking requires the target to be BikeHorn
    /// </summary>
    public class LockLockerInteraction : Interaction
    {
        [HideInInspector]
        public AccessPermission permissionToOpen;
        public Locker locker;

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Lock";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : AssetData.Get(InteractionIcons.Honk);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionSource source = interactionEvent.Source;
            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);

            if (!inRange)
            {
                return false;
            }

            return !(locker.Locked);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;
            IInteractionTarget target = interactionEvent.Target;

            if (source is IGameObjectProvider sourceGameObjectProvider
                && target is Locker)
            {
                var hands = sourceGameObjectProvider.GameObject.GetComponentInParent<Hands>();
                if (hands.Inventory.HasPermission(permissionToOpen))
                {
                    locker.SetLocked(true);

                    Debug.Log("Locked!");
                    return true;
                }
            }

            Debug.Log("No permission to lock!");
            return false;
        }
    }
}