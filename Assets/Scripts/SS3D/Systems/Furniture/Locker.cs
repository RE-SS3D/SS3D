using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Items;
using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Systems.Gamemodes;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class Locker : InteractionSourceNetworkBehaviour, IInteractionTarget
    {
        public IDPermission permissionToOpen;
        public bool Locked { private set; get; } = true;
        private bool Open = false;

        public bool CanOpenDoor()
        {
            return (!Locked && !Open);
        }

        public bool CanCloseDoor()
        {
            return (!Locked && Open);
        }

        public bool OpenDoor()
        {
            Debug.Log("Opened!");
            Open = true;
            return true;
        }

        public bool CloseDoor()
        {
            Debug.Log("Closed!");
            Open = false;
            return true;
        }

        public void SetLocked(bool locked)
        {
            Locked = locked;
        }

        IInteraction[] IInteractionTarget.CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            SimpleInteraction openInteraction =
                new SimpleInteraction("Open", AssetData.Get(InteractionIcons.Open),
                    CanOpenDoor, OpenDoor);

            SimpleInteraction closeInteraction =
                new SimpleInteraction("Close", AssetData.Get(InteractionIcons.Open),
                    CanCloseDoor, CloseDoor);

            UnlockLockerInteraction unlockLockerInteraction = new UnlockLockerInteraction();
            unlockLockerInteraction.permissionToOpen = permissionToOpen;
            unlockLockerInteraction.locker = this;

            LockLockerInteraction lockLockerInteraction = new LockLockerInteraction();
            lockLockerInteraction.permissionToOpen = permissionToOpen;
            lockLockerInteraction.locker = this;

            return new IInteraction[] { openInteraction, closeInteraction,
                unlockLockerInteraction, lockLockerInteraction };
        }
    }
}