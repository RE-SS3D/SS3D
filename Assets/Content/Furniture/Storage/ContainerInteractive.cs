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

        public ContainerDescriptor containerDescriptor;
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
    }
}