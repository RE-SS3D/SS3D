using SS3D.Core;
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
    public sealed class LockLockerInteraction : IInteraction, IClientInteractionSource
    {
        private static AssetHandle<Sprite> DefaultIconHandle;
        private static bool TryingToLoadIcon;
        
        public string Name;
        public Sprite Icon;
        private readonly IDPermission _permissionToUnlock;
        private readonly Locker _locker;

        public LockLockerInteraction(Locker locker, IDPermission permission)
        {
            _locker = locker;
            _permissionToUnlock = permission;

            if (DefaultIconHandle is { IsValid: true })
            {
                return;
            }

            AcquireDefaultIcon();
            UnityEngine.Application.quitting += OnApplicationQuit;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Lock Locker";
        }

        public string GetGenericName() => throw new NotImplementedException();

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : AssetLoader.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Open);
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

            return !_locker.IsLocked && !_locker.IsOpen;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;

            if (source is not IGameObjectProvider sourceGameObjectProvider)
            {
                return false;
            }

            Hands hands = sourceGameObjectProvider.GameObject.GetComponentInParent<Hands>();

            if (hands == null)
            {
                return true;
            }

            if (hands.Inventory.HasPermission(_permissionToUnlock))
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

        private static async void AcquireDefaultIcon()
        {
            if (TryingToLoadIcon || DefaultIconHandle is { IsValid: true } || SubSystems.TryGet(out AssetSubSystem assetSubSystem) || !assetSubSystem)
            {
                return;
            }
            
            TryingToLoadIcon = true;
            DefaultIconHandle = await assetSubSystem.AcquireAsync<Sprite>(InteractionIcons.Open);

            if (DefaultIconHandle is { IsValid: false })
            {
                ReleaseDefaultIcon();
            }
            
            TryingToLoadIcon = false;
        }

        private static void OnApplicationQuit()
        {
            ReleaseDefaultIcon();
            UnityEngine.Application.quitting -= OnApplicationQuit;
        }

        private static void ReleaseDefaultIcon()
        {
            DefaultIconHandle?.Dispose();
            DefaultIconHandle = null;
        }
    }
}