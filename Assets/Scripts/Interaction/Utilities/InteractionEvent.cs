using System;
using Interaction.Core;
using UnityEngine;
using UnityEngine.Events;
using Event = Interaction.Core.Event;

namespace Interaction.Utilities
{
    [RequireComponent(typeof(InteractionReceiver))]
    public class InteractionEvent : MonoBehaviour, IInteraction
    {
        [SerializeField] private string kind = "";

        [Serializable]
        public class UnityInteractionEvent : UnityEvent<GameObject> { }
        public UnityInteractionEvent receive;
        
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen(kind);
        }

        public bool Handle(Event e)
        {
            receive?.Invoke(e.sender);
            return true;
        }
        
    }
}