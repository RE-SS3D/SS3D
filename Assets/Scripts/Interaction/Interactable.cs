using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Interaction
{
    public class Interactable : MonoBehaviour
    {
        private readonly Dictionary<InteractionKind, List<IInteractable>> receivers = new Dictionary<InteractionKind, List<IInteractable>>();

        private void Start()
        {
            foreach (var interactable in GetComponents<IInteractable>())
                interactable.Advertise(this);
        }

        public void Subscribe(InteractionKind kind, IInteractable receiver)
        {
            if (!receivers.ContainsKey(kind))
                receivers.Add(kind, new List<IInteractable>());
            receivers[kind].Add(receiver);
        }
        public void Unsubscribe(InteractionKind kind, IInteractable receiver)
        {
            if (!receivers.ContainsKey(kind)) return;
            receivers[kind].Remove(receiver);
            if (receivers[kind].Count == 0) receivers.Remove(kind);
        }

        public void Trigger(InteractionEvent e)
        {
            foreach (var receiver in receivers[e.kind])
                receiver.Handle(e);
        }
    }
}