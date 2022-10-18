﻿using SS3D.Core.Behaviours;
using SS3D.Engine.Interactions;
using UnityEngine;

namespace SS3D.Interactions
{
    public abstract class InteractionTargetBehaviour : SpessBehaviour, IInteractionTarget, IGameObjectProvider
    {
        public GameObject GameObject => GameObjectCache;
        public abstract IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent);
    }
}