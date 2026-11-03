using SS3D.Core.Behaviours;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Owns the player's <see cref="Controls"/> and exposes the input arbiter.
    /// </summary>
    /// <remarks>
    /// Input state is derived, never counted. Callers push an <see cref="InputContext"/> or a
    /// suppression and receive an <see cref="IInputHandle"/>; disposing the handle removes exactly
    /// that request. All resolution lives in <see cref="InputArbiter"/>, which writes
    /// InputAction.enabled from the live request set, so dead keys, leaked input, and enabled/refcount
    /// desync are structurally impossible.
    /// </remarks>
    public sealed partial class InputSubSystem : SubSystem
    {
        public Controls Inputs { get; private set; }

        public float MouseSensitivity { get; private set; }

        /// <summary>Escape while a modal UI (machine interface) is open. Bound in code, arbitrated.</summary>
        public InputAction UiCancel => _uiCancel;

        /// <summary>Held while inspecting (Shift). Bound in code, arbitrated by the Gameplay context.</summary>
        public InputAction DetailedExamine => _detailedExamine;

        /// <summary>Toggles Help/Harm intent (F). Bound in code, arbitrated by the Gameplay context.</summary>
        public InputAction ToggleIntent => _toggleIntent;

        /// <summary>Opens local-speech compose (T). Bound in code, arbitrated by the Gameplay context.</summary>
        public InputAction OpenLocalSpeechCompose => _openLocalSpeechCompose;

        /// <summary>
        /// Opens the F1 debug window, which lists every <c>DebugTool</c>. Unbound in release builds, so the
        /// window cannot open there. Replaces the per-tool debug keys (H, P, I, M, Y, L, F2–F4, F6).
        /// </summary>
        public InputAction ToggleDebugMenu => _toggleDebugMenu;

        /// <summary>F8: the bug report dialog, from anywhere but a focused text field or the console.</summary>
        public InputAction OpenBugReport => _openBugReport;

        /// <summary>Tab: collapses or expands the round-end summary. Live wherever the player can move or look.</summary>
        public InputAction ToggleRoundSummary => _toggleRoundSummary;

        private InputActionMap _systemMap;
        private InputAction _uiCancel;
        private InputAction _detailedExamine;
        private InputAction _toggleIntent;
        private InputAction _openLocalSpeechCompose;
        private InputAction _toggleDebugMenu;
        private InputAction _openBugReport;
        private InputAction _toggleRoundSummary;

        private InputArbiter _arbiter;
        private Dictionary<InputContext, InputContextDefinition> _contexts;

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

            // Legacy hold-to-show objectives (Fade) and debug Spawn Cans both bound Tab in the
            // generated Controls asset. Erase those bindings — Tab is compose-only now.
            EraseAllBindings(Inputs.Other.Fade);
            EraseAllBindings(Inputs.Other.SpawnCans);
            // Unused Other/Attack and Other/Show Owner both sat on F; F is ToggleIntent now.
            EraseAllBindings(Inputs.Other.Attack);
            EraseAllBindings(Inputs.Other.ShowOwner);
            // Debug tools run from the F1 debug window now, which frees their letters for players to rebind.
            EraseAllBindings(Inputs.Other.SwapMinds);
            EraseAllBindings(Inputs.Other.ToggleRuntimeEditor);
            EraseAllBindings(Inputs.Other.Ragdoll);
            EraseAllBindings(Inputs.Other.SeeContainerContents);
            EraseAllBindings(Inputs.Other.ToggleSelectionDebug);
            EraseAllBindings(Inputs.Other.ToggleAtmosDebug);

            BuildSystemActions();

            List<InputAction> allActions = CollectAllActions();
            _contexts = BuildContexts();
            _arbiter = new InputArbiter(allActions, _contexts);

            // Always-on baseline; never released.
            _arbiter.PushContext(InputContext.Global);
        }

        private static void EraseAllBindings(InputAction action)
        {
            for (int i = action.bindings.Count - 1; i >= 0; i--)
            {
                action.ChangeBinding(i).Erase();
            }
        }

        #region Public API

        /// <summary>
        /// Pushes a context. While the returned handle is live it participates in resolving the active
        /// context (highest priority wins). Dispose to pop it.
        /// </summary>
        public IInputHandle PushContext(InputContext context) => _arbiter.PushContext(context);

        /// <summary>True while anything holds this context, even under a higher one (Menu under TextEntry).</summary>
        public bool IsContextPushed(InputContext context) => _arbiter.IsPushed(context);

        /// <summary>Raised after any context is pushed or released.</summary>
        public event System.Action ContextsChanged
        {
            add => _arbiter.ContextsChanged += value;
            remove => _arbiter.ContextsChanged -= value;
        }

        /// <summary>Suppresses every action in a map while the handle is live.</summary>
        public IInputHandle SuppressMap(InputActionMap map) => _arbiter.SuppressActions(map.actions);

        /// <summary>Suppresses a single action while the handle is live.</summary>
        public IInputHandle SuppressAction(InputAction action) => _arbiter.SuppressActions(new[] { action });

        /// <summary>
        /// Suppresses every action that contains a binding with the given control path (e.g.
        /// "&lt;Mouse&gt;/leftButton", "&lt;Mouse&gt;/scroll/y") while the handle is live.
        /// </summary>
        public IInputHandle SuppressBinding(string bindingPath) => _arbiter.SuppressBinding(bindingPath);

        #endregion

        #region Setup helpers

        private void BuildSystemActions()
        {
            _systemMap = new InputActionMap("System");
            _uiCancel = _systemMap.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            // Shift is examine-only (sprint is Caps Lock on Movement/Toggle Run).
            _detailedExamine = _systemMap.AddAction("DetailedExamine", InputActionType.Button);
            _detailedExamine.AddBinding("<Keyboard>/leftShift");
            _detailedExamine.AddBinding("<Keyboard>/rightShift");
            _toggleIntent = _systemMap.AddAction("ToggleIntent", InputActionType.Button, "<Keyboard>/f");
            _openLocalSpeechCompose = _systemMap.AddAction(
                "OpenLocalSpeechCompose", InputActionType.Button, "<Keyboard>/t");
            // Editor and development builds only (owner's call): release builds get no binding.
            _toggleDebugMenu = Debug.isDebugBuild
                ? _systemMap.AddAction("ToggleDebugMenu", InputActionType.Button, "<Keyboard>/f1")
                : _systemMap.AddAction("ToggleDebugMenu", InputActionType.Button);
            _openBugReport = _systemMap.AddAction("OpenBugReport", InputActionType.Button, "<Keyboard>/f8");
            _toggleRoundSummary = _systemMap.AddAction("ToggleRoundSummary", InputActionType.Button, "<Keyboard>/tab");
        }

        private List<InputAction> CollectAllActions()
        {
            List<InputAction> allActions = new();
            foreach (InputAction action in Inputs)
            {
                allActions.Add(action);
            }

            foreach (InputAction action in _systemMap.actions)
            {
                allActions.Add(action);
            }

            return allActions;
        }

        private Dictionary<InputContext, InputContextDefinition> BuildContexts()
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

            return new Dictionary<InputContext, InputContextDefinition>
            {
                // System actions only: menu toggle (in Other), open console, open build menu,
                // plus code-defined debug toggles that share Other's availability.
                [InputContext.Global] = new InputContextDefinition(
                    new[] { other },
                    new[] { consoleOpen, tileToggle, _toggleDebugMenu, _openBugReport, _toggleRoundSummary }),

                [InputContext.Gameplay] = new InputContextDefinition(
                    new[] { movement, camera, interactions, hotkeys, other },
                    new[]
                    {
                        consoleOpen, tileToggle, _detailedExamine, _toggleIntent,
                        _openLocalSpeechCompose, _toggleDebugMenu, _openBugReport, _toggleRoundSummary,
                    }),

                // Build menu: keep looking around and placing; drop world interactions/hotkeys.
                [InputContext.TileMenu] = new InputContextDefinition(
                    new[] { movement, camera, tile, other },
                    new[] { consoleOpen, _detailedExamine, _toggleDebugMenu, _openBugReport, _toggleRoundSummary }),

                // Map Editor: it polls Keyboard/Mouse directly for its own free-fly camera, so
                // Movement/Camera must be masked here too (unlike TileMenu) to avoid the normal
                // character/camera fighting it every frame.
                [InputContext.MapEditor] = new InputContextDefinition(
                    new[] { tile, other },
                    new[] { consoleOpen, _openBugReport, _toggleRoundSummary }),

                // Machine panel captures movement/camera; Escape closes via UiCancel (Other masked).
                [InputContext.MachineUI] = new InputContextDefinition(
                    new[] { hotkeys, interactions },
                    new[] { _uiCancel, _openBugReport }),

                // In-round lobby overlay: Other keeps Escape (ToggleMenu) live to close it.
                [InputContext.Menu] = new InputContextDefinition(
                    new[] { other },
                    new[] { _openBugReport }),

                // Bug report dialog: everything off; the dialog polls Escape and Ctrl+Enter itself.
                [InputContext.Dialog] = new InputContextDefinition(
                    System.Array.Empty<InputActionMap>(),
                    System.Array.Empty<InputAction>()),

                [InputContext.Console] = new InputContextDefinition(
                    new[] { console },
                    System.Array.Empty<InputAction>()),

                // Text field focused (local-speech compose): gameplay off; Tab is read from
                // Keyboard device in LocalSpeechBubbleController (UITK focus eats InputActions).
                [InputContext.TextEntry] = new InputContextDefinition(
                    System.Array.Empty<InputActionMap>(),
                    System.Array.Empty<InputAction>()),
            };
        }

        #endregion
    }
}
