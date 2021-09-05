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
    public class OpenableContainer : NetworkedOpenable
    {

        public ContainerDescriptor containerDescriptor;
        [SerializeField] private Sprite viewContainerIcon;

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            // The "open" state is changed via the object interaction
            // so we don't need to worry about that here, unless we can store stuff without needing it to open

            List<IInteraction> interactions = new List<IInteraction>();
            OpenInteraction openInteraction = new OpenInteraction(containerDescriptor);
            openInteraction.icon = containerDescriptor.OpenIcon;
            openInteraction.OpenStateChange += OnOpenStateChange;
            StoreInteraction storeInteraction = new StoreInteraction(containerDescriptor);
            storeInteraction.icon = containerDescriptor.StoreIcon;
            TakeInteraction takeInteraction = new TakeInteraction(containerDescriptor);
            takeInteraction.icon = containerDescriptor.TakeIcon;
            ViewContainerInteraction view = new ViewContainerInteraction(containerDescriptor){MaxDistance = containerDescriptor.MaxDistance, icon = viewContainerIcon};
            view.icon = containerDescriptor.ViewIcon;

            interactions.Insert(0, openInteraction);

            // Implicit or Normal the Store Interaction will always appear, but View only appears in Normal containers
            if (IsOpen() | !containerDescriptor.OnlyStoreWhenOpen)
            {
                switch (containerDescriptor.ContainerType)
                {
                    case ContainerType.Normal:
                        interactions.Insert(1, storeInteraction);
                        interactions.Insert(2, view);
                        break;
                    case ContainerType.Pile:
                        interactions.Insert(1, storeInteraction);
                        interactions.Insert(2, takeInteraction);
                        break;
                }
            }

            return interactions.ToArray();
        }
    }
}