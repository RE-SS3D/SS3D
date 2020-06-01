using SS3D.Engine.Interactions;
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
            return (interactionEvent.Target as IGameObjectProvider)?.GameObject?.GetComponent<Container>() != null;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            GameObject source = ((IGameObjectProvider)interactionEvent.Source.GetRootSource()).GameObject;
            GameObject target = ((IGameObjectProvider)interactionEvent.Target).GameObject;
            Inventory inventory = source.GetComponent<Inventory>();
            if (!inventory.HasContainer(target))
            {
                var attacher = source.AddComponent<ContainerAttachment>();
                attacher.container = target.GetComponent<Container>();
                attacher.inventory = inventory;
                attacher.range = MaxDistance;
            }
            else
            {
                inventory.RemoveContainer(target);
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