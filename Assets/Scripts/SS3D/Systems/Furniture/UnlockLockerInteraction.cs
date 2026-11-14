using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Logging;
using SS3D.Systems.Furniture;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Tile.MapEditor;
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

        public int Priority => 25;

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Unlock Locker";
        }

        public string GetGenericName() => "UnlockLocker";

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Unlock);
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

            if (!MayUseLock(interactionEvent))
            {
                return false;
            }

            return _locker.IsLocked && !_locker.IsOpen;
        }

        /// <summary>
        /// The ID check, skipped for someone with the map editor open: who has access stays the prefab's or the room's,
        /// but an editor user sets lockers up without carrying every ID (object-setup-persistence Q7).
        /// </summary>
        private bool MayUseLock(InteractionEvent interactionEvent) =>
            InteractionPermission.HasPermission(interactionEvent, _permissionToUnlock)
            || (interactionEvent.Source is IGameObjectProvider provider && MapEditorSubSystem.IsEditorUser(provider.GameObject));

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

            if (!MayUseLock(interactionEvent))
            {
                Log.Information(this, "No permission to unlock Locker!");

                return false;
            }

            Log.Information(this, "Locker has been unlocked!");
            _locker.IsLocked = false;

            return true;
        }
    }
}