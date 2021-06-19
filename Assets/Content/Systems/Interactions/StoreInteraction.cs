using SS3D.Content.Furniture;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using UnityEngine;
using SS3D.Engine.Inventory.UI;
using SS3D.Content.Furniture.Storage;

namespace SS3D.Content.Systems.Interactions
{
    public class StoreInteraction : IInteraction
    {
        public Sprite icon;

        public IClientInteraction CreateClient(InteractionEvent interactionEvent)
        {
            return null;
        }

        public virtual string GetName(InteractionEvent interactionEvent)
        {
            return "Store";
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

            var target = interactionEvent.Target.GetComponent<AttachedContainer>();
            if (interactionEvent.Source.Parent is Hands hands && target != null)
            {
                return !hands.SelectedHandEmpty && CanStore(interactionEvent.GetSourceItem(), target);
            }
            return false;
        }

        private bool CanStore(Item item, AttachedContainer target)
        {
            Container container = target.Container;
            return container.CouldStoreItem(item) && container.CouldHoldItem(item);
        }

        public virtual bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hands hands = (Hands) interactionEvent.Source.Parent;
            interactionEvent.Target.GetComponent<AttachedContainer>().Container.AddItem(hands.ItemInHand);
            CloseUIWhenStored(interactionEvent);
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

        /// <summary>
        /// Checks if the UI of the stored item is opened, if it is, then close it. 
        /// </summary>
        private void CloseUIWhenStored(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source is Item)
            {
                Item item = interactionEvent.Source as Item;
                GameObject gameObject = item.prefab; 
                if (gameObject.GetComponent<OpenableContainer>() != null)
                {
                    ContainerUi[] UIs = GameObject.FindObjectsOfType<ContainerUi>(); //That's probably terrible
                    foreach (ContainerUi UI in UIs)
                    {
                        if (UI.AttachedContainer == gameObject.GetComponent<AttachedContainer>())
                        {
                            UI.Close();
                        }
                    }
                }
            }
        }      
    }
}