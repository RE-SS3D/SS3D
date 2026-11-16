using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions.UI;
using SS3D.Systems.Selection;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Client-side armed interaction state and overlay for Tier 2/3 radial selections. Attaches into
    /// the shared <see cref="UiShellSubSystem"/> overlay layer instead of owning a private UIDocument.
    /// </summary>
    public sealed class ArmedInteractionSubSystem : SubSystem
    {
        public event Func<Selectable, ArmedTargetEvaluation> EvaluateTarget;

        [SerializeField] private StyleSheet _overlayStyleSheet;

        /// <summary>Shared by the throw-mode overlay, which uses the same reticle and chip look.</summary>
        public StyleSheet OverlayStyleSheet => _overlayStyleSheet;

        private ArmedInteractionOverlayView _overlayView;
        private SelectionSubSystem _selectionSystem;
        private ArmedInteractionState _state;

        public bool IsArmed => _state != null;

        public ArmedInteractionState CurrentState => _state;

        protected override void OnEnabled()
        {
            base.OnEnabled();

            // Selection lives on NetworkSystemsHub; Armed may OnEnable before Selection registers
            // (hub behaviour order) or during FishNet scene-object flash before the hub exists.
            if (TryResolveSelection())
            {
                _selectionSystem.OnSelectableChanged += HandleSelectableChanged;
            }
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            if (_selectionSystem != null)
            {
                _selectionSystem.OnSelectableChanged -= HandleSelectableChanged;
            }
        }

        protected override void OnDestroyed()
        {
            _overlayView?.Detach();
            _overlayView = null;
            base.OnDestroyed();
        }

        private void Update()
        {
            if (!IsArmed || !EnsureOverlayView())
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

            if (!EnsureOverlayView())
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
        }

        private void HandleSelectableChanged()
        {
            RefreshHoverState();
        }

        private void RefreshHoverState()
        {
            if (!IsArmed || _overlayView == null || !TryResolveSelection())
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

        private bool TryResolveSelection()
        {
            if (_selectionSystem != null)
            {
                return true;
            }

            return SubSystems.TryGet(out _selectionSystem);
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

            return $"{label} - {targetName}";
        }

        private bool EnsureOverlayView()
        {
            if (_overlayView != null)
            {
                return true;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem uiShell) || !uiShell.TryGetLayer(UiLayer.Overlay, out VisualElement layerRoot))
            {
                Debug.LogError("ArmedInteractionSubSystem could not find the UiShellSubSystem overlay layer.", this);
                return false;
            }

            // See RadialInteractionSubSystem for why this register is never paired with an unregister here.
            InputInterface.RegisterDocument(uiShell.Document);

            _overlayView = new ArmedInteractionOverlayView(_overlayStyleSheet);
            _overlayView.Attach(layerRoot);
            return true;
        }
    }
}
