using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [CreateAssetMenu(fileName = "ForwardInteraction", menuName = "Interaction/Forward Interaction", order = 0)]
    internal sealed class ForwardInteraction : SingularInteraction
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
            if (!e.forwardTo) return false;
            
            var newEvent = e;
            newEvent.forwardTo = null;
            newEvent.kind = to;
            newEvent.sender = Receiver.gameObject;

            if (triggerBlockOnFail)
            {
                e.forwardTo.Trigger(newEvent, null, () =>
                {
                    foreach (var block in blocks)
                    {
                        var blockEvent = e;
                        blockEvent.kind = block;
                        Debug.Log("Triggering "+block.name+" because "+to.name+" failed");
                        Receiver.Trigger(blockEvent);
                    }
                });
            }
            else
            {
                e.forwardTo.Trigger(newEvent);
            }

            return true;
        }
    }
}