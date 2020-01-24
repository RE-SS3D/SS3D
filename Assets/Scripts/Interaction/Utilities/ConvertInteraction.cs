using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [CreateAssetMenu(fileName = "ForwardInteraction", menuName = "Interaction/Forward Interaction", order = 0)]
    internal sealed class ConvertInteraction : Core.Interaction
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
            var receiver = Receiver.GetComponent<InteractionReceiver>();
            if (!receiver)
            {
                Debug.LogWarning($"There is no receiver on the interaction {Receiver.name}");
                return false;
            }

            e.kind = to;
            receiver.Trigger(e);

            return true;
        }
    }
}