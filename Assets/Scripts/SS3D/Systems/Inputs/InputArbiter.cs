using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Pure arbitration engine. Input state is derived, never counted: callers push a context or a
    /// suppression and get an <see cref="IInputHandle"/>; disposing removes exactly that request. The
    /// effective enabled state of every action is recomputed from the live request set and written by
    /// the single writer <see cref="Recompute"/>.
    /// </summary>
    /// <remarks>
    /// Decoupled from <see cref="InputSubSystem"/> and the generated <c>Controls</c> asset so it can be
    /// unit tested with synthetic <see cref="InputActionMap"/>s. Not thread-safe; call on the main thread.
    /// </remarks>
    public sealed class InputArbiter
    {
        private readonly List<InputAction> _allActions;
        private readonly Dictionary<InputContext, InputContextDefinition> _contexts;
        private readonly List<Request> _requests = new();
        private long _orderCounter;

        public InputArbiter(IEnumerable<InputAction> allActions, Dictionary<InputContext, InputContextDefinition> contexts)
        {
            _allActions = new List<InputAction>(allActions);
            _contexts = contexts;
        }

        /// <summary>Pushes a context; the highest-priority live context resolves as active.</summary>
        public IInputHandle PushContext(InputContext context)
        {
            return AddRequest(new Request
            {
                IsContext = true,
                Context = context,
                Order = _orderCounter++,
                Alive = true,
            });
        }

        /// <summary>Suppresses the given actions while the handle is live.</summary>
        public IInputHandle SuppressActions(IEnumerable<InputAction> actions)
        {
            return AddRequest(new Request
            {
                IsContext = false,
                SuppressTargets = new HashSet<InputAction>(actions),
                Order = _orderCounter++,
                Alive = true,
            });
        }

        /// <summary>Suppresses every action that contains a binding with the given control path.</summary>
        public IInputHandle SuppressBinding(string bindingPath)
        {
            return SuppressActions(_allActions.Where(a => a.bindings.Any(b => b.path == bindingPath)));
        }

        private IInputHandle AddRequest(Request request)
        {
            _requests.Add(request);
            Recompute();
            return new Handle(this, request);
        }

        private void Release(Request request)
        {
            if (!request.Alive)
            {
                return;
            }

            request.Alive = false;
            _requests.Remove(request);
            Recompute();
        }

        private void Recompute()
        {
            InputContextDefinition active = ResolveActiveContext();

            foreach (InputAction action in _allActions)
            {
                bool desired = active.Includes(action) && !IsSuppressed(action);

                if (desired && !action.enabled)
                {
                    action.Enable();
                }
                else if (!desired && action.enabled)
                {
                    action.Disable();
                }
            }
        }

        private InputContextDefinition ResolveActiveContext()
        {
            InputContextDefinition active = _contexts[InputContext.Global];
            int bestPriority = -1;
            long bestOrder = -1;

            foreach (Request request in _requests)
            {
                if (!request.IsContext)
                {
                    continue;
                }

                int priority = (int)request.Context;
                if (priority > bestPriority || (priority == bestPriority && request.Order > bestOrder))
                {
                    bestPriority = priority;
                    bestOrder = request.Order;
                    active = _contexts[request.Context];
                }
            }

            return active;
        }

        private bool IsSuppressed(InputAction action)
        {
            foreach (Request request in _requests)
            {
                if (!request.IsContext && request.SuppressTargets.Contains(action))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class Request
        {
            public bool IsContext;
            public InputContext Context;
            public HashSet<InputAction> SuppressTargets;
            public long Order;
            public bool Alive;
        }

        private sealed class Handle : IInputHandle
        {
            private readonly InputArbiter _arbiter;
            private readonly Request _request;

            public Handle(InputArbiter arbiter, Request request)
            {
                _arbiter = arbiter;
                _request = request;
            }

            public void Dispose()
            {
                _arbiter.Release(_request);
            }
        }
    }
}
