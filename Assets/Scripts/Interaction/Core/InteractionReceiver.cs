using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Interaction.Core
{
    [DisallowMultipleComponent]
    public sealed class InteractionReceiver : MonoBehaviour
    {
        [SerializeField] private SingularInteraction[] singularInteractions = new SingularInteraction[0];
        [SerializeField] private ContinuousInteraction[] continuousInteractions = new ContinuousInteraction[0];
        [Tooltip("Turns on logging of otherwise uninteresting events")]
        [SerializeField] private bool debug = false;
        
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
                    kind => SetBlocker(kind, interactable));

            foreach (var interaction in singularInteractions)
            {
                if (interaction == null) continue;
                var localInteraction = Instantiate(interaction);
                localInteraction.receiver = this;
                localInteraction.Setup(
                    kind => Subscribe(kind, localInteraction),
                    kind => SetBlocker(kind, localInteraction));
            }
            foreach (var interaction in continuousInteractions)
            {
                if (interaction == null) continue;
                var localInteraction = Instantiate(interaction);
                localInteraction.receiver = gameObject;
                localInteraction.Setup(
                    kind => Subscribe(kind, localInteraction),
                    kind => SetBlocker(kind, localInteraction));
            }
        }

        public bool IsListeningForContinuous(ContinuousInteraction interaction)
        {
            return continuousInteractions.ToList().Any(listener => listener.GetType() == interaction.GetType());
        }

        private void SetBlocker(InteractionKind kind, IBaseInteraction receiver)
        {
            if (!listeners.ContainsKey(receiver))
            {
                Debug.LogError($"{receiver} has no listener set on {this} when setting blocker for {kind}");
                return;
            }
            
            foreach (var listener in listeners[receiver])
            {
                if (!dependencies.ContainsKey(listener))
                {
                    dependencies.Add(listener, kind);
                }
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
        /// <param name="onFail">Action to be called if the event fails</param>
        public void Trigger(InteractionEvent e, Action onSuccess = null, Action onFail = null)
        {
            if (debug)
            {
                Debug.Log($"Triggered: {e.kind.name} on {name}");
            }
            
            if (onFail != null)
            {
                e.onFail = onFail;
            }
            if (onSuccess != null)
            {
                e.onSuccess = onSuccess;
            }

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

                if (skip.Contains(e.kind)) continue;
                if (!receivers.ContainsKey(e.kind))
                {
                    e.onFail?.Invoke();
                    continue;
                }

                if (debug)
                {
                    Debug.Log($"{e} @ {transform.name}");
                }
                
                foreach (var receiver in receivers[e.kind])
                {
                    switch (receiver)
                    {
                        case ISingularInteraction singular:
                        {
                            var result = singular.Handle(e);
                            if (result)
                            {
                                e.onSuccess?.Invoke();
                                
                                if (dependencies.TryGetValue(e.kind, out var dependency))
                                {
                                    skip.Add(dependency);
                                }
                            }
                            else
                            {
                                e.onFail?.Invoke();
                            }

                            break;
                        }
                        case IContinuousInteraction continuous:
                            StartCoroutine(HandleContinuous(e, continuous));
                            break;
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