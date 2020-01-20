using System;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(Interactable))]
    public class Storable : MonoBehaviour, IInteractable
    {
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen("use");
        }

        public bool Handle(InteractionEvent e)
        {
            if (!e.forwardTo) return false;
            e.forwardTo.Trigger(e.Forward("store", transform));

            return true;
        }
    }
}