using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(InteractionReceiver))]
    public class Openable : MonoBehaviour, ISingularInteraction
    {
        [SerializeField] private InteractionKind kind = null;
        
        public bool open;
        
        public void Setup(Action<InteractionKind> listen, Action<InteractionKind> blocks)
        {
            listen(kind);
        }

        public void Reset() { }

        public bool Handle(InteractionEvent e)
        {
            open = !open;
            
            Debug.Log($"{name} is now {(open ? "open" : "closed")}");

            return true;
        }
    }
}