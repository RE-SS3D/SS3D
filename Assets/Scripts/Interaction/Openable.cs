using System;
using Interaction.Core;
using UnityEngine;

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

        public bool Handle(InteractionEvent e)
        {
            open = !open;
            
            Debug.Log($"{name} is now {(open ? "open" : "closed")}");

            return true;
        }
    }
}