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

            List<IInteraction> interactions = base.GenerateInteractionsFromTarget(interactionEvent).ToList();
            StoreInteraction storeInteraction = new StoreInteraction();
            TakeInteraction takeInteraction = new TakeInteraction();
            ViewContainerInteraction view = new ViewContainerInteraction {MaxDistance = containerDescriptor.MaxDistance, icon = viewContainerIcon};

        // Implicit or Normal the Store Interaction will always appear, but View only appears in Normal containers
            if (IsOpen() | !containerDescriptor.OnlyStoreWhenOpen)
            {
                switch (containerDescriptor.ContainerType)
                {
                    case ContainerType.Normal:
                        interactions.Insert(0, storeInteraction);
                        interactions.Insert(1, view);
                        break;
                    case ContainerType.Pile:
                        interactions.Insert(0, storeInteraction);
                        interactions.Insert(1, takeInteraction);
                        break;
                }
            }

            return interactions.ToArray();
        }
    }
}