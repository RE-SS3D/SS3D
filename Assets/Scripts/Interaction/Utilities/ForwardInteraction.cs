using System;
using UnityEngine;
using Event = Interaction.Core.Event;

namespace Interaction.Utilities
{
    [CreateAssetMenu(fileName = "ForwardInteraction", menuName = "Interaction/Forward Interaction", order = 0)]
    public class ForwardInteraction : Core.Interaction
    {
        [SerializeField] private string from = "";
        [SerializeField] private string to = "";
        
        public override void Setup(Action<string> listen, Action<string> blocks)
        {
            listen(from);
        }

        public override bool Handle(Event e)
        {
            if (!e.forwardTo) return false;
            e.forwardTo.Trigger(e.Forward(to, Receiver));

            return true;
        }
    }
}