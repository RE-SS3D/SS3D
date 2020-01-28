using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [RequireComponent(typeof(InteractionReceiver))]
    internal sealed class ForwardInteractionAs : MonoBehaviour, ISingularInteraction
    {
        [SerializeField] private InteractionKind from = null;
        [SerializeField] private InteractionKind to = null;
        [SerializeField] private InteractionKind[] blocks = new InteractionKind[0];
        
        public void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(from);
            foreach (var block in this.blocks)
            {
                blocks(block);
            }
        }

        public bool Handle(Core.InteractionEvent e)
        {
            if (!e.forwardTo) return false;

            var newEvent = e;
            newEvent.forwardTo = null;
            newEvent.kind = to;
            newEvent.sender = gameObject;
            
            e.forwardTo.Trigger(newEvent);

            return true;
        }
    }
}