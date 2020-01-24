using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [RequireComponent(typeof(InteractionReceiver))]
    internal sealed class ForwardInteractionAs : MonoBehaviour, IInteraction
    {
        [SerializeField] private InteractionKind from = null;
        [SerializeField] private InteractionKind to = null;
        
        public void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(from);
        }

        public bool Handle(Core.InteractionEvent e)
        {
            if (!e.forwardTo) return false;
            e.forwardTo.Trigger(e.Forward(to, gameObject));

            return true;
        }
    }
}