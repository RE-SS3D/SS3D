using System;
using System.Collections.Generic;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions.UI;
using SS3D.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Controls the UI Toolkit radial interaction menu.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class RadialInteractionSubSystem : SubSystem
    {
        public event Action<IInteraction> OnInteractionSelected;

        [SerializeField] private UIDocument _document;
        [SerializeField] private StyleSheet _menuStyleSheet;
        [SerializeField] private Sprite _missingIcon;
        [SerializeField] private Sprite _closeIconSprite;
        [SerializeField] private int _maxPetals = 12;

        private RadialInteractionMenuView _menuView;
        private List<IInteraction> _interactions;
        private InteractionEvent _event;
        private Controls.InteractionsActions _controls;
        private InputSubSystem _inputSystem;
        private bool _overlayReady;
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

            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

#if UNITY_EDITOR
            EnsureEditorAssets();
#endif
            ShutdownDocument();
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _controls = _inputSystem.Inputs.Interactions;
            InputInterface.RegisterDocument(_document);
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
            InputInterface.UnregisterDocument(_document);
            _menuView?.Detach();
            ShutdownDocument();
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
            if (!hasInteractions || !EnsureDocumentActive())
            {
                return;
            }

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
                ShutdownDocument();
                return;
            }

            _menuView.InteractionSelected -= HandleInteractionSelected;
            _menuView.CloseRequested -= HandleCloseRequested;
            _menuView.Hide(() =>
            {
                ResetInteractionsMenu();
                ShutdownDocument();
            });
        }

        private void ResetInteractionsMenu()
        {
            _interactions?.Clear();
            _event = null;
        }

        private bool EnsureDocumentActive()
        {
            if (_document == null)
            {
                return false;
            }

            if (!_document.enabled)
            {
                _document.enabled = true;
                _overlayReady = false;
            }

            return EnsureOverlay();
        }

        private bool EnsureOverlay()
        {
            if (_overlayReady && _menuView != null)
            {
                return true;
            }

            VisualElement root = _document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("RadialInteractionSubSystem requires PanelSettings on UIDocument.", this);
                return false;
            }

            _menuView?.Detach();
            _menuView = new RadialInteractionMenuView(_menuStyleSheet, _missingIcon, _closeIconSprite, _maxPetals);
            _menuView.Attach(root);
            _menuView.InteractionSelected += HandleInteractionSelected;
            _menuView.CloseRequested += HandleCloseRequested;
            _overlayReady = true;
            return true;
        }

        private void ShutdownDocument()
        {
            _overlayReady = false;
            _menuView?.Detach();
            _menuView = null;

            if (_document != null)
            {
                _document.enabled = false;
            }
        }

#if UNITY_EDITOR
        private void EnsureEditorAssets()
        {
            if (_menuStyleSheet == null)
            {
                _menuStyleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/Interactions/RadialInteractionMenu/RadialInteractionMenu.uss");
            }

            if (_document != null && _document.panelSettings == null)
            {
                _document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    "Assets/Content/Systems/UI/Interactions/RadialInteractionMenu/HudOverlayPanelSettings.asset");
            }
        }
#endif
    }
}
