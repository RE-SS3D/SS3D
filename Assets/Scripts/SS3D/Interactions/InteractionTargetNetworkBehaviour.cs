﻿using SS3D.Core.Behaviours;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    public abstract class InteractionTargetNetworkBehaviour : NetworkedSpessBehaviour, IInteractionTarget, IGameObjectProvider
    {
        public abstract IInteraction[] GetTargetInteractions(InteractionEvent interactionEvent);
        public GameObject GameObject => GameObjectCache;
    }
}