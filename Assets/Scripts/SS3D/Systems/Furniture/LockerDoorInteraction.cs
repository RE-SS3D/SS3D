using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    public class LockerDoorInteraction : Interaction
    {
        private readonly Locker _locker;

        public LockerDoorInteraction(Locker locker)
        {
            _locker = locker;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return !string.IsNullOrEmpty(Name) ? Name : "Open or Close Locker";
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Open;
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (_locker.IsLocked)
            {
                return false;
            }
            
            return InteractionExtensions.RangeCheck(interactionEvent);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            _locker.IsOpen = !_locker.IsOpen;

            return true;
        }
    }
}