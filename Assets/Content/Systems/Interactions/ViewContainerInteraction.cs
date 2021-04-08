﻿using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using UnityEngine;

namespace SS3D.Content.Systems.Interactions
{
    public class ViewContainerInteraction : IInteraction
    {
        public Sprite icon;

        public float MaxDistance { get; set; }
        
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "View Container";
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            var container = interactionEvent.Target.GetComponent<AttachedContainer>();
            if (container == null)
            {
                return false;
            }
            var inventory = interactionEvent.Source.GetComponentInTree<Inventory>();
            if (inventory == null)
            {
                return false;
            }
            
            Entity entity = interactionEvent.Source.GetEntity();
            if (entity == null)
            {
                return false;
            }
            return !inventory.HasContainer(container) && entity.CanInteract(container.gameObject);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            var inventory = interactionEvent.Source.GetComponentInTree<Inventory>();
            var attachedContainer = interactionEvent.Target.GetComponent<AttachedContainer>();
            
            inventory.OpenContainer(attachedContainer);

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