using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions.UI;
using SS3D.Systems.Selection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Client-side armed interaction state and overlay for Tier 2/3 radial selections.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ArmedInteractionSubSystem : SubSystem
    {
        public event Func<Selectable, ArmedTargetEvaluation> EvaluateTarget;

        [SerializeField] private UIDocument _document;
        [SerializeField] private StyleSheet _overlayStyleSheet;

        private ArmedInteractionOverlayView _overlayView;
        private SelectionSubSystem _selectionSystem;
        private ArmedInteractionState _state;
        private bool _overlayReady;

        public bool IsArmed => _state != null;

        public ArmedInteractionState CurrentState => _state;

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
            InputInterface.RegisterDocument(_document);

            _selectionSystem = SubSystems.Get<SelectionSubSystem>();
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            _selectionSystem.OnSelectableChanged += HandleSelectableChanged;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            _selectionSystem.OnSelectableChanged -= HandleSelectableChanged;
        }

        protected override void OnDestroyed()
        {
            InputInterface.UnregisterDocument(_document);
            _overlayView?.Detach();
            ShutdownDocument();
            base.OnDestroyed();
        }

        private void Update()
        {
            if (!IsArmed || !EnsureDocumentActive())
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cancel();
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            _overlayView.UpdateCursorPosition(mousePosition);
            RefreshHoverState();
        }

        public void Arm(IInteraction interaction, InteractionEvent originEvent, InteractionTier tier, string label)
        {
            _state = new ArmedInteractionState
            {
                Interaction = interaction,
                OriginEvent = originEvent,
                Tier = tier,
                Label = label,
            };

            if (!EnsureDocumentActive())
            {
                _state = null;
                return;
            }

            string chipLabel = BuildChipLabel(label, originEvent);
            _overlayView.Show(chipLabel);
            RefreshHoverState();
        }

        public void Cancel()
        {
            if (!IsArmed)
            {
                return;
            }

            _state = null;
            _overlayView?.Hide();
            ShutdownDocument();
        }

        private void HandleSelectableChanged()
        {
            RefreshHoverState();
        }

        private void RefreshHoverState()
        {
            if (!IsArmed || _overlayView == null)
            {
                return;
            }

            if (!_selectionSystem.TryGetCurrentSelectable(out Selectable selectable))
            {
                _overlayView.SetTargetState(false, false);
                return;
            }

            ArmedTargetEvaluation evaluation = EvaluateHover(selectable);
            _overlayView.SetTargetState(evaluation.HasTarget, evaluation.IsValid);
        }

        private ArmedTargetEvaluation EvaluateHover(Selectable selectable)
        {
            if (EvaluateTarget == null)
            {
                return ArmedTargetEvaluation.None;
            }

            return EvaluateTarget.Invoke(selectable);
        }

        private static string BuildChipLabel(string label, InteractionEvent originEvent)
        {
            string targetName = "target";
            if (originEvent?.Target is IGameObjectProvider provider)
            {
                targetName = provider.GameObject.name;
            }

            return $"{label} — {targetName} →";
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
            if (_overlayReady && _overlayView != null)
            {
                return true;
            }

            VisualElement root = _document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("ArmedInteractionSubSystem requires PanelSettings on UIDocument.", this);
                return false;
            }

            _overlayView?.Detach();
            _overlayView = new ArmedInteractionOverlayView(_overlayStyleSheet);
            _overlayView.Attach(root);
            _overlayReady = true;
            return true;
        }

        private void ShutdownDocument()
        {
            _overlayReady = false;
            _overlayView?.Detach();
            _overlayView = null;

            if (_document != null)
            {
                _document.enabled = false;
            }
        }

#if UNITY_EDITOR
        private void EnsureEditorAssets()
        {
            if (_overlayStyleSheet == null)
            {
                _overlayStyleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/Interactions/ArmedInteractionOverlay/ArmedInteractionOverlay.uss");
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
