using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [CreateAssetMenu(fileName = "ForwardInteraction", menuName = "Interaction/Forward Interaction", order = 0)]
    internal sealed class SendToTarget : SingularInteraction
    {
        [SerializeField] private InteractionKind from = null;
        [SerializeField] private InteractionKind to = null;
        [SerializeField] private InteractionKind[] blocks = new InteractionKind[0];
        [SerializeField] private bool triggerBlockOnFail = false;
        
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
            if (!e.target)
            {
                TriggerBlock(e);
                return false;
            }
            
            var newEvent = e;
            newEvent.target = null;
            newEvent.kind = to;
            newEvent.sender = Receiver.gameObject;

            if (triggerBlockOnFail)
            {
                e.target.Trigger(newEvent, null, () =>
                {
                    TriggerBlock(e);
                });
            }
            else
            {
                e.target.Trigger(newEvent);
            }

            return true;
        }

        private void TriggerBlock(Core.InteractionEvent e)
        {
            foreach (var block in blocks)
            {
                var blockEvent = e;
                blockEvent.kind = block;
                Receiver.Trigger(blockEvent);
            }
        }
    }
}