using SS3D.Core.Behaviours;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Owns the player's <see cref="Controls"/> and arbitrates which actions are live.
    /// </summary>
    /// <remarks>
    /// Input state is derived, never counted. Callers push an <see cref="InputContext"/> or a
    /// suppression and receive an <see cref="IInputHandle"/>; disposing the handle removes exactly
    /// that request. The effective enabled state of every action is recomputed from the current set
    /// of live requests and written by a single writer (<see cref="Recompute"/>), so dead keys,
    /// leaked input, and enabled/refcount desync are structurally impossible.
    /// </remarks>
    public sealed class InputSubSystem : SubSystem
    {
        public Controls Inputs { get; private set; }

        public float MouseSensitivity { get; private set; }

        /// <summary>Escape while a modal UI (machine interface) is open. Bound in code, arbitrated.</summary>
        public InputAction UiCancel => _uiCancel;

        /// <summary>Held while inspecting (Shift). Bound in code, arbitrated by the Gameplay context.</summary>
        public InputAction DetailedExamine => _detailedExamine;

        private InputActionMap _systemMap;
        private InputAction _uiCancel;
        private InputAction _detailedExamine;

        private readonly List<Request> _requests = new();
        private readonly List<InputAction> _allActions = new();
        private Dictionary<InputContext, ContextDefinition> _contexts;
        private long _orderCounter;

        protected override void OnAwake()
        {
            DontDestroyOnLoad(transform.gameObject);

            base.OnAwake();

            Setup();
        }

        private void Setup()
        {
            MouseSensitivity = 0.001f;
            Inputs = new Controls();

            BuildSystemActions();
            CacheAllActions();
            BuildContexts();

            // Always-on baseline; never released.
            PushContext(InputContext.Global);
        }

        #region Public API

        /// <summary>
        /// Pushes a context. While the returned handle is live it participates in resolving the
        /// active context (highest priority wins). Dispose to pop it.
        /// </summary>
        public IInputHandle PushContext(InputContext context)
        {
            Request request = new()
            {
                IsContext = true,
                Context = context,
                Order = _orderCounter++,
                Alive = true,
            };

            return AddRequest(request);
        }

        /// <summary>Suppresses every action in a map while the handle is live.</summary>
        public IInputHandle SuppressMap(InputActionMap map)
        {
            return SuppressActions(map.actions);
        }

        /// <summary>Suppresses a single action while the handle is live.</summary>
        public IInputHandle SuppressAction(InputAction action)
        {
            return SuppressActions(new[] { action });
        }

        /// <summary>
        /// Suppresses every action that contains a binding with the given control path (e.g.
        /// "&lt;Mouse&gt;/leftButton", "&lt;Mouse&gt;/scroll/y") while the handle is live.
        /// </summary>
        public IInputHandle SuppressBinding(string bindingPath)
        {
            IEnumerable<InputAction> targets = _allActions.Where(a => a.bindings.Any(b => b.path == bindingPath));
            return SuppressActions(targets);
        }

        #endregion

        #region Setup helpers

        private void BuildSystemActions()
        {
            _systemMap = new InputActionMap("System");
            _uiCancel = _systemMap.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            _detailedExamine = _systemMap.AddAction("DetailedExamine", InputActionType.Button);
            _detailedExamine.AddBinding("<Keyboard>/leftShift");
            _detailedExamine.AddBinding("<Keyboard>/rightShift");
        }

        private void CacheAllActions()
        {
            _allActions.Clear();
            foreach (InputAction action in Inputs)
            {
                _allActions.Add(action);
            }

            foreach (InputAction action in _systemMap.actions)
            {
                _allActions.Add(action);
            }
        }

        private void BuildContexts()
        {
            InputActionMap camera = Inputs.Camera.Get();
            InputActionMap movement = Inputs.Movement.Get();
            InputActionMap console = Inputs.Console.Get();
            InputActionMap hotkeys = Inputs.Hotkeys.Get();
            InputActionMap other = Inputs.Other.Get();
            InputActionMap tile = Inputs.TileCreator.Get();
            InputActionMap interactions = Inputs.Interactions.Get();

            InputAction consoleOpen = Inputs.Console.Open;
            InputAction tileToggle = Inputs.TileCreator.ToggleMenu;
            InputAction sendChat = Inputs.Other.SendChatMessage;

            _contexts = new Dictionary<InputContext, ContextDefinition>
            {
                // System actions only: menu toggle (in Other), open console, open build menu.
                [InputContext.Global] = new ContextDefinition(
                    new[] { other },
                    new[] { consoleOpen, tileToggle }),

                [InputContext.Gameplay] = new ContextDefinition(
                    new[] { movement, camera, interactions, hotkeys, other },
                    new[] { consoleOpen, tileToggle, _detailedExamine }),

                // Build menu: keep looking around and placing; drop world interactions/hotkeys.
                [InputContext.TileMenu] = new ContextDefinition(
                    new[] { movement, camera, tile, other },
                    new[] { consoleOpen, _detailedExamine }),

                // Machine panel captures movement/camera; Escape closes via UiCancel (Other masked).
                [InputContext.MachineUI] = new ContextDefinition(
                    new[] { hotkeys, interactions },
                    new[] { _uiCancel }),

                [InputContext.Console] = new ContextDefinition(
                    new[] { console },
                    System.Array.Empty<InputAction>()),

                // Typing: everything off except sending the chat message that is being typed.
                [InputContext.TextEntry] = new ContextDefinition(
                    System.Array.Empty<InputActionMap>(),
                    new[] { sendChat }),
            };
        }

        #endregion

        #region Internal arbitration

        private IInputHandle SuppressActions(IEnumerable<InputAction> actions)
        {
            Request request = new()
            {
                IsContext = false,
                SuppressTargets = new HashSet<InputAction>(actions),
                Order = _orderCounter++,
                Alive = true,
            };

            return AddRequest(request);
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
            ContextDefinition active = ResolveActiveContext();

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

        private ContextDefinition ResolveActiveContext()
        {
            ContextDefinition active = _contexts[InputContext.Global];
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

        #endregion

        private sealed class ContextDefinition
        {
            private readonly HashSet<InputActionMap> _maps;
            private readonly HashSet<InputAction> _actions;

            public ContextDefinition(IEnumerable<InputActionMap> maps, IEnumerable<InputAction> actions)
            {
                _maps = new HashSet<InputActionMap>(maps);
                _actions = new HashSet<InputAction>(actions);
            }

            public bool Includes(InputAction action)
            {
                return _maps.Contains(action.actionMap) || _actions.Contains(action);
            }
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
            private readonly InputSubSystem _system;
            private readonly Request _request;

            public Handle(InputSubSystem system, Request request)
            {
                _system = system;
                _request = request;
            }

            public void Dispose()
            {
                _system.Release(_request);
            }
        }
    }
}
