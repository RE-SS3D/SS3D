using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Items;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Traits.TraitCategories;
using UnityEngine;
using FishNet.Object.Synchronizing;

namespace SS3D.Systems.Furniture
{
    public class Locker : InteractionSourceNetworkBehaviour, IInteractionTarget
    {
        public AccessPermission permissionToOpen;
        public bool Locked { private set; get; } = true;

        public void SetLocked(bool locked)
        {
            Locked = locked;
        }

        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            UnlockLockerInteraction unlockLockerInteraction = new UnlockLockerInteraction();
            unlockLockerInteraction.permissionToOpen = permissionToOpen;
            unlockLockerInteraction.locker = this;

            LockLockerInteraction lockLockerInteraction = new LockLockerInteraction();
            lockLockerInteraction.permissionToOpen = permissionToOpen;
            lockLockerInteraction.locker = this;

            return new IInteraction[] { unlockLockerInteraction, lockLockerInteraction };
        }
    }
}