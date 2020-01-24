using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction.Core
{
    [DisallowMultipleComponent]
    public sealed class InteractionReceiver : MonoBehaviour
    {
        [SerializeField] private SingularInteraction[] singularInteractions = new SingularInteraction[0];
        [SerializeField] private ContinuousInteraction[] continuousInteractions = new ContinuousInteraction[0];
        
        private readonly Dictionary<InteractionKind, List<IBaseInteraction>> receivers = new Dictionary<InteractionKind, List<IBaseInteraction>>();
        private readonly Dictionary<IBaseInteraction, List<InteractionKind>> listeners = new Dictionary<IBaseInteraction, List<InteractionKind>>();
        private readonly Dictionary<InteractionKind, InteractionKind> dependencies = new Dictionary<InteractionKind, InteractionKind>();
        private readonly List<InteractionEvent> eventQueue = new List<InteractionEvent>();
        private readonly List<InteractionReceiver> waiting = new List<InteractionReceiver>();

        private Coroutine handlerCoroutine = null;

        private bool IsClear => handlerCoroutine == null;

        private void Start()
        {
            foreach (var interactable in GetComponents<IBaseInteraction>())
                interactable.Setup(
                    kind => Subscribe(kind, interactable),
                    kind => SetBlockage(kind, interactable));

            foreach (var interaction in singularInteractions)
            {
                var localInteraction = Instantiate(interaction);
                localInteraction.receiver = gameObject;
                localInteraction.Setup(
                    kind => Subscribe(kind, localInteraction),
                    kind => SetBlockage(kind, localInteraction));
            }
            foreach (var interaction in continuousInteractions)
            {
                var localInteraction = Instantiate(interaction);
                localInteraction.receiver = gameObject;
                localInteraction.Setup(
                    kind => Subscribe(kind, localInteraction),
                    kind => SetBlockage(kind, localInteraction));
            }
        }

        private void SetBlockage(InteractionKind kind, IBaseInteraction receiver)
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
        /// Useful when setting up interactions from non unity classes.<br/>
        /// You probably don't need this if you don't know what you're doing.
        /// </summary>
        /// <param name="kind">The kind to subscribe to</param>
        /// <param name="receiver">The interaction to subscribe</param>
        public void Subscribe(InteractionKind kind, IBaseInteraction receiver)
        {
            if (!receivers.ContainsKey(kind))
                receivers.Add(kind, new List<IBaseInteraction>());
            receivers[kind].Add(receiver);
            
            if (!listeners.ContainsKey(receiver))
                listeners.Add(receiver, new List<InteractionKind>());
            listeners[receiver].Add(kind);
        }
        
        /// <summary>
        /// Manually unsubscribe an interaction from an event kind.<br/>
        /// Useful when setting up interactions from non unity classes.<br/>
        /// You probably don't need this if you don't know what you're doing.
        /// </summary>
        /// <param name="kind">The kind to unsubscribe from</param>
        /// <param name="receiver">The interaction to unsubscribe</param>
        public void Unsubscribe(InteractionKind kind, IBaseInteraction receiver)
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
                for (var i = 0; i < waiting.Count; i++)
                {
                    yield return new WaitUntil(() => waiting[i].IsClear);
                }

                waiting.Clear();
            }
            else
            {
                yield return null;
            }

            var skip = new HashSet<InteractionKind>();
            for (var i = 0; i < eventQueue.Count; i++)
            {
                var e = eventQueue[i];
                
                if (!receivers.ContainsKey(e.kind)) continue;
                if (skip.Contains(e.kind)) continue;
                Debug.Log($"{e} @ {transform.name}");
                foreach (var receiver in receivers[e.kind])
                {
                    if (receiver is ISingularInteraction singular)
                    {
                        if (singular.Handle(e) && dependencies.TryGetValue(e.kind, out var dependency))
                        {
                            skip.Add(dependency);
                        }
                    }

                    if (receiver is IContinuousInteraction continuous)
                    {
                        if (e.runWhile != null)
                        {
                            StartCoroutine(HandleContinuous(e, continuous));
                        }
                    }
                }
            }

            eventQueue.Clear();
            
            handlerCoroutine = null;
        }

        private IEnumerator HandleContinuous(InteractionEvent e, IContinuousInteraction continuous)
        {
            var enumerator = continuous.Handle(e);

            if (!enumerator.MoveNext()) yield break;
            
            do
            {
                if (e.runWhile != null && !e.runWhile(e)) break;
                yield return enumerator.Current;
            } while (enumerator.MoveNext());
        }
    }
}