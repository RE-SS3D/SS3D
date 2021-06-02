using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using UnityEngine;

namespace SS3D.Content.Furniture.Storage
{
    public class StorageContainer : NetworkedOpenable
    {
        public bool OnlyStoreWhenOpen = false;
        public float MaxDistance = 5f;

        [SerializeField] private Sprite viewContainerIcon;

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
	    // The "open" state is changed via the object interaction
	    // so we don't need to worry about that here, unless we can store stuff without needing it to open

            List<IInteraction> interactions = base.GenerateInteractionsFromTarget(interactionEvent).ToList();
            StoreInteraction storeInteraction = new StoreInteraction();
            ViewContainerInteraction view = new ViewContainerInteraction {MaxDistance = MaxDistance, icon = viewContainerIcon};

	    // if its open, we add the store interaction with the view interaction
            if (IsOpen())
            {
                interactions.Insert(0, storeInteraction);
                interactions.Insert(1, view);
            }
	    // if we can store items without it being open, we generate both interactions
            else if (!OnlyStoreWhenOpen)
            {
                interactions.Add(storeInteraction);
                interactions.Add(view);
            }

            return interactions.ToArray();
        }
    }
}