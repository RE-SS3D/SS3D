using System;
using Interaction.Core;
using UnityEngine;
using Event = Interaction.Core.Event;

namespace Interaction
{
    [RequireComponent(typeof(InteractionReceiver))]
    public class Openable : MonoBehaviour, IInteraction
    {
        public bool open;
        
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen("open");
        }

        public bool Handle(Event e)
        {
            open = !open;
            
            Debug.Log($"{name} is now {(open ? "open" : "closed")}");

            return true;
        }
    }
}