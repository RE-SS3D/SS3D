using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [CreateAssetMenu(fileName = "ForwardInteraction", menuName = "Interaction/Forward Interaction", order = 0)]
    internal sealed class ForwardInteraction : Core.SingularInteraction
    {
        [SerializeField] private InteractionKind from = null;
        [SerializeField] private InteractionKind to = null;
        [SerializeField] private InteractionKind[] blocks = new InteractionKind[0];
        
        public override void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(from);
            foreach (var block in this.blocks)
            {
                blocks(block);
            }
        }

        public override bool Handle(Core.InteractionEvent e)
        {
            if (!e.forwardTo) return false;
            e.forwardTo.Trigger(e.Forward(to, Receiver));

            return true;
        }
    }
}