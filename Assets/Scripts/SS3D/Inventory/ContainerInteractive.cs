using System.Collections.Generic;
using SS3D.Interactions;
using UnityEngine;

namespace SS3D.Inventory
{
    public class ContainerInteractive : NetworkedOpenable
    {

        [HideInInspector] public ContainerDescriptor containerDescriptor;
        private Sprite _viewContainerIcon;

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            if (containerDescriptor.hasCustomInteraction)
            {
                return new IInteraction[0];
            }

            List<IInteraction> interactions = new List<IInteraction>();

            StoreInteraction storeInteraction = new StoreInteraction(containerDescriptor);
            storeInteraction.Icon = containerDescriptor.storeIcon;
            TakeInteraction takeInteraction = new TakeInteraction(containerDescriptor);
            takeInteraction.Icon = containerDescriptor.takeIcon;
            ViewContainerInteraction view = new ViewContainerInteraction(containerDescriptor){MaxDistance = containerDescriptor.maxDistance, Icon = _viewContainerIcon};
            view.Icon = containerDescriptor.viewIcon;

            // Pile or Normal the Store Interaction will always appear, but View only appears in Normal containers
            if (IsOpen() | !containerDescriptor.onlyStoreWhenOpen | !containerDescriptor.isOpenable)
            {
                if (containerDescriptor.hasUi)
                {
                    interactions.Add(storeInteraction);
                    interactions.Add(view);
                }
                else
                {
                    interactions.Add(storeInteraction);
                    interactions.Add(takeInteraction);
                }           
            }

            if (containerDescriptor.isOpenable)
            {
                OpenInteraction openInteraction = new(containerDescriptor);
                openInteraction.icon = containerDescriptor.openIcon;
                openInteraction.OnOpenStateChanged += OpenStateChanged;
                interactions.Add(openInteraction);
            }

            return interactions.ToArray();
        }

        
        protected override void OpenStateChanged(object sender, bool e)
        {  
            base.OpenStateChanged(sender, e);
        }

        /// <summary>
        /// recursively closes all container UI when the root container is closed.
        /// This is potentially very slow when there's a lot of containers and items as it calls get component for every items in every container.
        /// A faster solution could be to use unity game tag and to tag every object with a container as such.
        /// Keeping track in Container of the list of objects that are containers would make it really fast.
        /// </summary>
        private void closeUis()
        {
            if (containerDescriptor.containerUi != null)
            {
                containerDescriptor.containerUi.Close();
            }
            
            // We check for each item if they are interactive containers
            foreach(Item item in containerDescriptor.attachedContainer.Container.Items)
            {
                ContainerInteractive[] containerInteractives = item.GameObject.GetComponents<ContainerInteractive>();
                // if the item is an interactive container, we call this method again on it.
                if (containerInteractives != null)
                {
                    foreach(ContainerInteractive c in containerInteractives)
                    {
                        c.closeUis();
                    }                   
                }
            }
        }

        protected override void SyncOpenState(bool oldVal, bool newVal, bool asServer)
        {
            base.SyncOpenState(oldVal, newVal, asServer);
            if (!newVal)
            {
                closeUis();
            }
        }
    }
}