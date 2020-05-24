﻿using System;
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
        private Animator animator;

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            List<IInteraction> interactions = base.GenerateInteractions(interactionEvent).ToList();
            StoreInteraction storeInteraction = new StoreInteraction {OnlyWhenOpen = OnlyStoreWhenOpen};
            ViewContainerInteraction view = new ViewContainerInteraction {MaxDistance = MaxDistance};
            if (IsOpen())
            {
                interactions.Insert(0, storeInteraction);
                interactions.Insert(1, view);
            }
            else if (!OnlyStoreWhenOpen)
            {
                interactions.Add(storeInteraction);
                interactions.Add(view);
            }

            return interactions.ToArray();
        }
    }
}