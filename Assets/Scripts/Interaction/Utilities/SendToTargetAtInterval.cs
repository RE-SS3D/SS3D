using System;
using System.Collections;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [CreateAssetMenu(fileName = "SentAtInterval", menuName = "Interaction/Send to target at interval", order = 0)]
    internal sealed class SendToTargetAtInterval : ContinuousInteraction
    {
        [SerializeField] private float interval = 1.0f;
        [SerializeField] private InteractionKind from = null;
        [SerializeField] private InteractionKind to = null;
        
        public override void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(from);
        }

        public override IEnumerator Handle(Core.InteractionEvent e)
        {
            if (!e.target)
            {
                yield break;
            }
            
            var newEvent = e;
            newEvent.target = null;
            newEvent.kind = to;
            newEvent.sender = Receiver.gameObject;

            while (e.runWhile(e))
            {
                e.target.Trigger(newEvent);
                yield return new WaitForSeconds(interval);
            }
        }
    }
}