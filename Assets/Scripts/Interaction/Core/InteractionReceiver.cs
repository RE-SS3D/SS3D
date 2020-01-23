using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.Core
{
    [DisallowMultipleComponent]
    public sealed class InteractionReceiver : MonoBehaviour
    {
        [SerializeField] private Interaction[] interactions = new Interaction[0];
        
        private readonly Dictionary<string, List<IInteraction>> receivers = new Dictionary<string, List<IInteraction>>();
        private readonly Dictionary<IInteraction, List<string>> listeners = new Dictionary<IInteraction, List<string>>();
        private readonly Dictionary<string, string> dependencies = new Dictionary<string, string>();
        private readonly List<InteractionEvent> eventQueue = new List<InteractionEvent>();
        private readonly List<InteractionReceiver> waiting = new List<InteractionReceiver>();

        private Coroutine handlerCoroutine = null;

        private bool IsClear => handlerCoroutine == null;

        private void Start()
        {
            foreach (var interactable in GetComponents<IInteraction>())
                interactable.Setup(
                    kind => Subscribe(kind, interactable),
                    kind => SetBlockage(kind, interactable));

            foreach (var interaction in interactions)
            {
                var localInteraction = Instantiate(interaction);
                localInteraction.receiver = gameObject;
                localInteraction.Setup(
                    kind => Subscribe(kind, localInteraction),
                    kind => SetBlockage(kind, localInteraction));
            }
        }

        private void SetBlockage(string kind, IInteraction receiver)
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

        /// <summary>
        /// Manually subscribe an interaction to an event kind.<br/>
        /// You probably don't need this if you don't know what you're doing
        /// </summary>
        /// <param name="kind">The kind to subscribe to</param>
        /// <param name="receiver">The interaction to subscribe</param>
        public void Subscribe(string kind, IInteraction receiver)
        {
            if (!receivers.ContainsKey(kind))
                receivers.Add(kind, new List<IInteraction>());
            receivers[kind].Add(receiver);
            
            if (!listeners.ContainsKey(receiver))
                listeners.Add(receiver, new List<string>());
            listeners[receiver].Add(kind);
        }
        
        /// <summary>
        /// Manually unsubscribe an interaction from an event kind.<br/>
        /// You probably don't need this if you don't know what you're doing
        /// </summary>
        /// <param name="kind">The kind to unsubscribe from</param>
        /// <param name="receiver">The interaction to unsubscribe</param>
        public void Unsubscribe(string kind, IInteraction receiver)
        {
            if (receivers.ContainsKey(kind))
            {
                receivers[kind].Remove(receiver);
                if (receivers[kind].Count == 0)
                {
                    receivers.Remove(kind);
                }
            }
            if (listeners.ContainsKey(receiver))
            {
                listeners[receiver].Remove(kind);
                if (listeners[receiver].Count == 0)
                {
                    listeners.Remove(receiver);
                }
            }
        }

        /// <summary>
        /// Add an `InteractionEvent` to the event queue to be handled by subscribed interactions
        /// </summary>
        /// <param name="e">The event to be triggered</param>
        public void Trigger(InteractionEvent e)
        {
            if (dependencies.TryGetValue(e.kind, out var dependency))
            {
                var index = eventQueue.FindIndex(ev => ev.kind == dependency);
                
                if (index == -1)
                {
                    eventQueue.Add(e);
                }
                else
                {
                    eventQueue.Insert(index, e);
                }
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
                foreach (var receiver in waiting)
                {
                    yield return new WaitUntil(() => receiver.IsClear);
                }

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