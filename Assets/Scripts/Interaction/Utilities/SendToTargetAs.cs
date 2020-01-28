using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [RequireComponent(typeof(InteractionReceiver))]
    internal sealed class SendToTargetAs : MonoBehaviour, ISingularInteraction
    {
        [SerializeField] private InteractionKind from = null;
        [SerializeField] private InteractionKind to = null;
        [SerializeField] private InteractionKind[] blocks = new InteractionKind[0];
        [SerializeField] private bool triggerBlockOnFail = false;
        
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
            if (!e.target)
            {
                TriggerBlock(e);
                return false;
            }
            
            var newEvent = e;
            newEvent.target = null;
            newEvent.kind = to;
            newEvent.sender = gameObject;

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
            var receiver = gameObject.GetComponent<InteractionReceiver>();

            if (!receiver)
            {
                Debug.LogError($"Cannot trigger event on {name} because it has no InteractionReceiver");
                return;
            }
            
            foreach (var block in blocks)
            {
                var blockEvent = e;
                blockEvent.kind = block;
                receiver.Trigger(blockEvent);
            }
        }
    }
}