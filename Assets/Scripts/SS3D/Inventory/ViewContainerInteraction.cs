using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Inventory.Extensions;
using SS3D.Systems.Entities;
using UnityEngine;

namespace SS3D.Inventory
{
    public class ViewContainerInteraction : IInteraction
    {
        public Sprite Icon;

        public float MaxDistance { get; set; }

        public readonly ContainerDescriptor ContainerDescriptor;

        public ViewContainerInteraction(ContainerDescriptor containerDescriptor)
        {
            this.ContainerDescriptor = containerDescriptor;
        }
        
        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "View " + ContainerDescriptor.containerName;
        }

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon;
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            var container = ContainerDescriptor.attachedContainer;
            if (container == null)
            {
                return false;
            }
            
            var inventory = interactionEvent.Source.GetComponentInTree<SS3D.Inventory.Inventory>();
            if (inventory == null)
            {
                return false;
            }
            
            PlayerControllable entity = interactionEvent.Source.GetComponentInTree<PlayerControllable>();
            if (entity == null)
            {
                return false;
            }
            return !inventory.HasContainer(container) && entity.GetComponent<Hands>().CanInteract(container.gameObject);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            var inventory = interactionEvent.Source.GetComponentInTree<SS3D.Inventory.Inventory>();
            var attachedContainer = ContainerDescriptor.attachedContainer;
            
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