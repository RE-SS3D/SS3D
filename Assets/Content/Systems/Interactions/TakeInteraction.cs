﻿using SS3D.Content.Furniture;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    // This Interaction takes the first available item inside a container
    public class TakeInteraction : IInteraction
    {
        public Sprite icon;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public virtual string GetName(InteractionEvent interactionEvent)
        {
            return "Take";
        }

        public virtual Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public virtual bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

        // Will only appear if the current hand is empty and the container isn't empty
            var target = interactionEvent.Target.GetComponent<AttachedContainer>();
            if (interactionEvent.Source is Hands hands && target != null)
            {
                return hands.SelectedHandEmpty && !target.Container.Empty;
            }

            return false;
        }

        public virtual bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hands hands = (Hands) interactionEvent.Source;
            Item PickupItem = interactionEvent.Target.GetComponent<AttachedContainer>().Container.StoredItems[0].Item;
            if (PickupItem != null)
            {
                hands.Pickup(PickupItem);
            }

            return false;
        }

        public bool Update(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }

        public void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            throw new System.NotImplementedException();
        }
    }
}