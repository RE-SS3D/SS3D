using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Click Open/Close for airlocks. Opening requires ID access; closing does not.
    /// </summary>
    public sealed class AirLockDoorInteraction : IInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;

        private readonly AirLockOpener _airLock;

        public AirLockDoorInteraction(AirLockOpener airLock)
        {
            _airLock = airLock;
        }

        public int Priority => _airLock.IsOpen ? 15 : 30;

        public string GetName(InteractionEvent interactionEvent)
        {
            return !string.IsNullOrEmpty(Name) ? Name : (_airLock.IsOpen ? "Close" : "Open");
        }

        public string GetGenericName() => "OpenAirlock";

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

            if (!_airLock.IsOpen && !_airLock.IsPowered)
            {
                return false;
            }

            return true;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Entity entity = interactionEvent.Source.GetComponentInParent<Entity>();
            HumanInventory inventory = entity != null ? entity.GetComponent<HumanInventory>() : null;
            if (_airLock.IsOpen)
            {
                _airLock.ServerCloseFromInteraction(inventory);
                return true;
            }

            _airLock.TryServerOpenFromInteraction(inventory);
            return true;
        }
    }
}
