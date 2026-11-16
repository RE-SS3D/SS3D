using System;
using System.Collections.Generic;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions.UI;
using SS3D.UI.Shell;
using SS3D.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Controls the UI Toolkit radial interaction menu. Attaches into the shared
    /// <see cref="UiShellSubSystem"/> overlay layer instead of owning a private UIDocument.
    /// </summary>
    public sealed class RadialInteractionSubSystem : SubSystem
    {
        public event Action<IInteraction> OnInteractionSelected;

        [SerializeField] private StyleSheet _menuStyleSheet;
        [SerializeField] private Sprite _missingIcon;
        [SerializeField] private Sprite _closeIconSprite;
        [SerializeField] private int _maxPetals = 12;

        private RadialInteractionMenuView _menuView;
        private List<IInteraction> _interactions;
        private InteractionEvent _event;
        private Controls.InteractionsActions _controls;
        private InputSubSystem _inputSystem;
        private IInputHandle _leftButtonSuppress;

        public float MenuHeight => RadialInteractionMenuView.MenuDiameter;

        /// <summary>
        /// Suppresses LMB actions while the radial menu is held open.
        /// </summary>
        public void SuppressLeftButtonForMenu()
        {
            _leftButtonSuppress ??= _inputSystem.SuppressBinding("<Mouse>/leftButton");
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            _inputSystem = SubSystems.Get<InputSubSystem>();
            _controls = _inputSystem.Inputs.Interactions;
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            _controls.ViewInteractions.canceled += HandleDisappear;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            _controls.ViewInteractions.canceled -= HandleDisappear;
        }

        protected override void OnDestroyed()
        {
            ReleaseLeftButtonSuppression();
            DetachMenuView();
            base.OnDestroyed();
        }

        public void SetInteractions(List<IInteraction> interactions, InteractionEvent interactionEvent, Vector3 mousePosition)
        {
            _interactions = interactions;
            _event = interactionEvent;
        }

        public void ShowInteractionsMenu()
        {
            bool hasInteractions = _event != null && _interactions != null && !_interactions.IsNullOrEmpty();
            if (!hasInteractions || !EnsureMenuView())
            {
                return;
            }

            // Disappear unsubscribes to avoid double-fires during close; the UiShell-backed view
            // is reused (unlike the old private UIDocument path that destroyed on hide), so each
            // show must re-bind or petal/close clicks silently no-op after the first close.
            BindMenuViewHandlers();

            Vector2 screenPos = Mouse.current.position.ReadValue();
            _menuView.Show(_interactions, _event, screenPos);
        }

        private void HandleInteractionSelected(IInteraction interaction)
        {
            _menuView.InteractionSelected -= HandleInteractionSelected;
            ReleaseLeftButtonSuppression();
            Disappear();
            OnInteractionSelected?.Invoke(interaction);
        }

        private void HandleCloseRequested()
        {
            ReleaseLeftButtonSuppression();
            Disappear();
        }

        private void HandleDisappear(InputAction.CallbackContext callbackContext)
        {
            ReleaseLeftButtonSuppression();
            Disappear();
        }

        private void ReleaseLeftButtonSuppression()
        {
            _leftButtonSuppress?.Dispose();
            _leftButtonSuppress = null;
        }

        private void Disappear()
        {
            if (_menuView == null)
            {
                return;
            }

            _menuView.InteractionSelected -= HandleInteractionSelected;
            _menuView.CloseRequested -= HandleCloseRequested;
            _menuView.Hide(ResetInteractionsMenu);
        }

        private void ResetInteractionsMenu()
        {
            _interactions?.Clear();
            _event = null;
        }

        private bool EnsureMenuView()
        {
            if (_menuView != null)
            {
                return true;
            }

            return AttachMenuView();
        }

        private bool AttachMenuView()
        {
            if (!SubSystems.TryGet(out UiShellSubSystem uiShell) || !uiShell.TryGetLayer(UiLayer.Overlay, out VisualElement layerRoot))
            {
                Debug.LogError("RadialInteractionSubSystem could not find the UiShellSubSystem overlay layer.", this);
                return false;
            }

            // UiShellSubSystem's document is shared and outlives this surface (DontDestroyOnLoad) — register is
            // idempotent and intentionally never unregistered here; unregistering on this surface's teardown
            // would stop input queries from seeing the document while other surfaces (e.g. armed overlay) still use it.
            InputInterface.RegisterDocument(uiShell.Document);

            _menuView = new RadialInteractionMenuView(_menuStyleSheet, _missingIcon, _closeIconSprite, _maxPetals);
            _menuView.Attach(layerRoot);
            BindMenuViewHandlers();
            return true;
        }

        private void BindMenuViewHandlers()
        {
            if (_menuView == null)
            {
                return;
            }

            // Idempotent: safe if already bound or if Disappear already cleared them.
            _menuView.InteractionSelected -= HandleInteractionSelected;
            _menuView.CloseRequested -= HandleCloseRequested;
            _menuView.InteractionSelected += HandleInteractionSelected;
            _menuView.CloseRequested += HandleCloseRequested;
        }

        private void DetachMenuView()
        {
            if (_menuView == null)
            {
                return;
            }

            _menuView.InteractionSelected -= HandleInteractionSelected;
            _menuView.CloseRequested -= HandleCloseRequested;
            _menuView.Detach();
            _menuView = null;
        }
    }
}
