using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using UnityEngine;

namespace SS3D.Content.Furniture.Storage
{
    public class OpenableContainer : NetworkedOpenable
    {
        [HideInInspector] public ContainerType StorageType = ContainerType.Normal;

        /// <summary>
        /// Does this container have a UI or can only be deposited/taken
        /// </summary>
        [SerializeField] private bool Pile = false;

        /// <summary>
        /// Does the container have to be open for items to be placed
        /// </summary>
        public bool OnlyStoreWhenOpen = false;

        public float MaxDistance = 5f;

        [SerializeField] private Sprite viewContainerIcon;

        void OnValidate()
        {
            // Openable Containers can never be Hidden
            if (Pile)
            {
                StorageType = ContainerType.Pile;
            } else
            {
                StorageType = ContainerType.Normal;
            }
        }

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
	    // The "open" state is changed via the object interaction
	    // so we don't need to worry about that here, unless we can store stuff without needing it to open

            List<IInteraction> interactions = base.GenerateInteractions(interactionEvent).ToList();
            StoreInteraction storeInteraction = new StoreInteraction();
            TakeInteraction takeInteraction = new TakeInteraction();
            ViewContainerInteraction view = new ViewContainerInteraction {MaxDistance = MaxDistance, icon = viewContainerIcon};

        // Implicit or Normal the Store Interaction will always appear, but View only appears in Normal containers
            if (IsOpen() | !OnlyStoreWhenOpen)
            {
                switch (StorageType)
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