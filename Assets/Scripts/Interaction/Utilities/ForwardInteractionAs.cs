using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Utilities
{
    [RequireComponent(typeof(InteractionReceiver))]
    internal sealed class ForwardInteractionAs : MonoBehaviour, IInteraction
    {
        [SerializeField] private string from = "";
        [SerializeField] private string to = "";
        
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen(from);
        }

        public bool Handle(Core.InteractionEvent e)
        {
            if (!e.forwardTo) return false;
            e.forwardTo.Trigger(e.Forward(to, gameObject));

            return true;
        }
    }
}