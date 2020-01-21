using System;
using Interaction.Core;
using Inventory.Custom;
using UnityEngine;
using Event = Interaction.Core.Event;

namespace Interaction
{
    [RequireComponent(typeof(InteractionReceiver))]
    public class Placeable : MonoBehaviour, IInteraction
    {
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen("use");
        }

        public bool Handle(Event e)
        {
            if (!e.forwardTo) return false;
            e.forwardTo.Trigger(e.Forward("place", gameObject));

            return true;
        }
    }
}