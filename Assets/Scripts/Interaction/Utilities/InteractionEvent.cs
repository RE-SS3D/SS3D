using System;
using Interaction.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Interaction.Utilities
{
    /// <summary>
    /// Used to emit a Unity event when an `InteractionEvent` of a certain kind is triggerec.
    /// </summary>
    [RequireComponent(typeof(InteractionReceiver))]
    public sealed class InteractionEvent : MonoBehaviour, IInteraction
    {
        [SerializeField] private string kind = "";

        [Serializable]
        public class UnityInteractionEvent : UnityEvent<GameObject> { }
        public UnityInteractionEvent receive;
        
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen(kind);
        }

        public bool Handle(Core.InteractionEvent e)
        {
            receive?.Invoke(e.sender);
            return true;
        }
        
    }
}