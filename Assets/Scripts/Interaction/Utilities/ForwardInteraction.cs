using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [CreateAssetMenu(fileName = "ForwardInteraction", menuName = "Interaction/Forward Interaction", order = 0)]
    internal sealed class ForwardInteraction : Core.Interaction
    {
        [SerializeField] private InteractionKind from = null;
        [SerializeField] private InteractionKind to = null;
        
        public override void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(from);
        }

        public override bool Handle(Core.InteractionEvent e)
        {
            if (!e.forwardTo) return false;
            e.forwardTo.Trigger(e.Forward(to, Receiver));

            return true;
        }
    }
}