using System;
using Interaction.Core;
using UnityEngine;
using Event = Interaction.Core.Event;

namespace Interaction.Utilities
{
    [RequireComponent(typeof(InteractionReceiver))]
    public class ForwardInteractionAs : MonoBehaviour, IInteraction
    {
        [SerializeField] private string from = "";
        [SerializeField] private string to = "";
        
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen(from);
        }

        public bool Handle(Event e)
        {
            if (!e.forwardTo) return false;
            e.forwardTo.Trigger(e.Forward(to, gameObject));

            return true;
        }
    }
}