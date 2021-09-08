using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using UnityEngine;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Furniture.Storage
{
    public class ContainerInteractive : NetworkedOpenable
    {

        [HideInInspector] public ContainerDescriptor containerDescriptor;
        private Sprite viewContainerIcon;

        protected override void Start()
        {   
            if (containerDescriptor.isOpenable)
            {
                animator = GameObject.GetComponent<Animator>();
            }     
        }

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        { 
            List<IInteraction> interactions = new List<IInteraction>();
            if (containerDescriptor.isOpenable)
            {
                OpenInteraction openInteraction = new OpenInteraction(containerDescriptor);
                openInteraction.icon = containerDescriptor.openIcon;
                openInteraction.OpenStateChange += OnOpenStateChange;
                interactions.Add(openInteraction);
            }

            StoreInteraction storeInteraction = new StoreInteraction(containerDescriptor);
            storeInteraction.icon = containerDescriptor.storeIcon;
            TakeInteraction takeInteraction = new TakeInteraction(containerDescriptor);
            takeInteraction.icon = containerDescriptor.takeIcon;
            ViewContainerInteraction view = new ViewContainerInteraction(containerDescriptor){MaxDistance = containerDescriptor.maxDistance, icon = viewContainerIcon};
            view.icon = containerDescriptor.viewIcon;

            // Pile or Normal the Store Interaction will always appear, but View only appears in Normal containers
            if (IsOpen() | !containerDescriptor.onlyStoreWhenOpen | !containerDescriptor.isInteractive)
            {
                switch (containerDescriptor.containerType)
                {
                    case ContainerType.Normal:
                        interactions.Add(storeInteraction);
                        interactions.Add(view);
                        break;
                    case ContainerType.Pile:
                        interactions.Add(storeInteraction);
                        interactions.Add(takeInteraction);
                        break;
                }
            }

            return interactions.ToArray();
        }

        
        protected override void OnOpenStateChange(object sender, bool e)
        {
            // this is not called from the client for some reasons
            Debug.Log("container changing state !");
            base.OnOpenStateChange(sender, e);
            if (!e)
            {
                closeUis();
            }
        }

        /// <summary>
        /// recursively closes all container UI when the root container is closed.
        /// This is potentially very slow as it calls get component for every items in every container.
        /// A faster solution could be to use unity game tag and to tag every object with a container as such.
        /// Keeping track in Container of the list of items that are containers would make it really fast.
        /// </summary>
        private void closeUis()
        {
            if(containerDescriptor.containerUi != null)
            {
                containerDescriptor.containerUi.Close();
            }
            
            foreach(Item item in containerDescriptor.attachedContainer.Container.Items)
            {
                ContainerInteractive containerInteractive = item.GameObject.GetComponent<ContainerInteractive>();
                if (containerInteractive != null)
                {
                    containerInteractive.closeUis();
                }
            }
        }
    }
}