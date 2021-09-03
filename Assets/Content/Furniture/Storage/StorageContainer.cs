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

    public class StorageContainer : InteractionTargetNetworkBehaviour
    {
        public ContainerDescriptor containerDescriptor;

        [SerializeField] private Sprite viewContainerIcon;

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
	    // The "open" state is changed via the object interaction
	    // so we don't need to worry about that here, unless we can store stuff without needing it to open

            List<IInteraction> interactions = new List<IInteraction>();
            StoreInteraction storeInteraction = new StoreInteraction(containerDescriptor);
            TakeInteraction takeInteraction = new TakeInteraction(containerDescriptor);
            ViewContainerInteraction view = new ViewContainerInteraction(containerDescriptor) {MaxDistance = containerDescriptor.MaxDistance, icon = viewContainerIcon};

            switch (containerDescriptor.ContainerType)
            {
            // Normal Containers don't have a Take Interaction since their UI can be opened
                case ContainerType.Normal:
                    interactions.Insert(0, storeInteraction);
                    interactions.Insert(1, view);
                    break;
            // Pile Containers don't have an UI, the only way to pick up items is taking one by one
                case ContainerType.Pile:
                    interactions.Insert(0, storeInteraction);
                    interactions.Insert(1, takeInteraction);
                    break;
            }

            return interactions.ToArray();
        }
    }
}