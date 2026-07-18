using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class LockerDoorInteraction : IInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;
        private readonly Locker _locker;

        public LockerDoorInteraction(Locker locker)
        {
            _locker = locker;
        }

        public int Priority => _locker.IsOpen ? 15 : 30;

        public string GetName(InteractionEvent interactionEvent)
        {
            return !string.IsNullOrEmpty(Name) ? Name : "Open or Close Locker";
        }

        public string GetGenericName() => "OpenLocker";

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Open);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (_locker.IsLocked)
            {
                return false;
            }

            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            bool wasOpen = _locker.IsOpen;
            _locker.IsOpen = !wasOpen;

            // Closing the door must tear down any open storage panels (server closes viewers → UI).
            if (wasOpen)
            {
                _locker.CloseStorageUIs();
            }

            return true;
        }
    }
}