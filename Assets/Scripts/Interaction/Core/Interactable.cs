using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.Core
{
    public class Interactable : MonoBehaviour
    {
        private readonly Dictionary<string, List<IInteractable>> receivers = new Dictionary<string, List<IInteractable>>();
        private readonly Dictionary<IInteractable, List<string>> listeners = new Dictionary<IInteractable, List<string>>();
        private readonly Dictionary<string, string> dependencies = new Dictionary<string, string>();
        private readonly List<InteractionEvent> eventQueue = new List<InteractionEvent>();
        private readonly List<Interactable> waiting = new List<Interactable>();

        private Coroutine handlerCoroutine;

        public bool IsClear => handlerCoroutine == null;

        private void Start()
        {
            foreach (var interactable in GetComponents<IInteractable>())
                interactable.Setup(
                    kind => Subscribe(kind, interactable),
                    kind => SetBlockage(kind, interactable));
        }

        private void SetBlockage(string kind, IInteractable receiver)
        {
            if (!listeners.ContainsKey(receiver))
            {
                Debug.LogError($"{receiver} has no listener set on {this} when setting blocker for {kind}");
                return;
            }
            
            foreach (var listener in listeners[receiver])
            {
                dependencies.Add(listener, kind);
            }
        }

        public void Subscribe(string kind, IInteractable receiver)
        {
            if (!receivers.ContainsKey(kind))
                receivers.Add(kind, new List<IInteractable>());
            receivers[kind].Add(receiver);
            
            if (!listeners.ContainsKey(receiver))
                listeners.Add(receiver, new List<string>());
            listeners[receiver].Add(kind);
        }
        public void Unsubscribe(string kind, IInteractable receiver)
        {
            if (receivers.ContainsKey(kind))
            {
                receivers[kind].Remove(receiver);
                if (receivers[kind].Count == 0) receivers.Remove(kind);
            }
            if (listeners.ContainsKey(receiver))
            {
                listeners[receiver].Remove(kind);
                if (listeners[receiver].Count == 0) listeners.Remove(receiver);
            }
        }

        public void Trigger(InteractionEvent e)
        {
            if (dependencies.TryGetValue(e.kind, out var dependency))
            {
                var index = eventQueue.FindIndex(ev => ev.kind == dependency);
                
                if (index == -1)
                    eventQueue.Add(e);
                else
                    eventQueue.Insert(index, e);
            }
            else
            {
                eventQueue.Add(e);
            }

            if (e.waitFor) waiting.Add(e.waitFor);
            if (handlerCoroutine == null) handlerCoroutine = StartCoroutine(HandleEvents());
        }

        private IEnumerator HandleEvents()
        {
            if (waiting.Count > 0)
            {
                for (var i = 0; i < waiting.Count; i++)
                    yield return new WaitUntil(() => waiting[i].IsClear);

                waiting.Clear();
            }
            else
            {
                yield return null;
            }

            var skip = new HashSet<string>();
            foreach (var e in eventQueue) 
            {
                if (!receivers.ContainsKey(e.kind)) continue;
                if (skip.Contains(e.kind)) continue;
                Debug.Log($"{e} @ {transform.name}");
                foreach (var receiver in receivers[e.kind])
                {
                    if (receiver.Handle(e) && dependencies.TryGetValue(e.kind, out var dependency))
                    {
                        skip.Add(dependency);
                    }
                }
            }
            eventQueue.Clear();
            
            handlerCoroutine = null;
        }
    }
}