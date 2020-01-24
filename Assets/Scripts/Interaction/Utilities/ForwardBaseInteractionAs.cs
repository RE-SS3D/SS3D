using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [RequireComponent(typeof(InteractionReceiver))]
    internal sealed class ForwardBaseInteractionAs : MonoBehaviour, ISingularInteraction
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
            e.forwardTo.Trigger(e.Forward(to, gameObject));

            return true;
        }
    }
}