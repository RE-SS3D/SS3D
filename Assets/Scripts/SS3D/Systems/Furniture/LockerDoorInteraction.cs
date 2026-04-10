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
        private static AssetHandle<Sprite> DefaultIconHandle;
        private static bool TryingToLoadDefaultIcon;

        public string Name;
        public Sprite Icon;
        private readonly Locker _locker;

        public LockerDoorInteraction(Locker locker)
        {
            _locker = locker;

            if (!TryingToLoadDefaultIcon && !DefaultIconHandle)
            {
                AcquireDefaultIcon();
            }
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return !string.IsNullOrEmpty(Name) ? Name : "Open or Close Locker";
        }

        public string GetGenericName() => throw new System.NotImplementedException();

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : DefaultIconHandle?.Asset;
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
            _locker.IsOpen = !_locker.IsOpen;

            return true;
        }

        private static async void AcquireDefaultIcon()
        {
            TryingToLoadDefaultIcon = true;
            DefaultIconHandle = await new AssetRequest<Sprite>(InteractionIcons.Open).LoadAsync();

            if (!DefaultIconHandle)
            {
                AssetHandle.Release(ref DefaultIconHandle);
            }
            else
            {
                UnityEngine.Application.quitting += OnApplicationQuit;
            }

            TryingToLoadDefaultIcon = false;
        }

        private static void OnApplicationQuit()
        {
            AssetHandle.Release(ref DefaultIconHandle);
            UnityEngine.Application.quitting -= OnApplicationQuit;
        }
    }
}