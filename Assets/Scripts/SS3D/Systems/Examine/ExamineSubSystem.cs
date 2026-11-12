using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Selection;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// The Examine System allows additional detail of items to be displayed when
    /// the cursor hovers over them. The particular information displayed is item
    /// and requirement dependant, and may take different formats.
    /// </summary>
    public class ExamineSubSystem : NetworkSubSystem
    {
        public event ExaminableChangedHandler OnExaminableChanged;
        public event ExaminableChangedHandler OnDetailedExamineRequested;

        /// <summary>
        /// Fired by <see cref="SS3D.Systems.Interactions.InteractionController"/> on Shift+Click over a
        /// character — kept here (rather than a direct reference) so the interactions layer never needs
        /// to depend on the UI-layer character-examine window that consumes this.
        /// </summary>
        public event ExaminableChangedHandler OnCharacterWindowRequested;

        public delegate void ExaminableChangedHandler(IExaminable examinable);

        /// <summary>Fired when <see cref="CursorLabelOverride"/> changes.</summary>
        public event System.Action OnCursorLabelOverrideChanged;

        /// <summary>
        /// While non-null, the cursor badge shows this text instead of examining what is under the pointer, and
        /// detailed examine stays off; empty hides the badge. The Map Editor sets it to what it is about to place.
        /// </summary>
        public string CursorLabelOverride { get; private set; }

        public void SetCursorLabelOverride(string label)
        {
            if (label == CursorLabelOverride)
            {
                return;
            }

            CursorLabelOverride = label;
            OnCursorLabelOverrideChanged?.Invoke();
        }

        private SelectionSubSystem _selectionSystem;
        private ItemFallbackExaminable _lastFallback;
        private bool _selectionEventsBound;

        protected override void OnEnabled()
        {
            base.OnEnabled();
            TryBindSelectionEvents();
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            UnsubscribeSelectionEvents();
        }

        private void Update()
        {
            // SelectionSubSystem may register after this NetworkSubSystem enables (hub component /
            // FishNet enable order). One-shot OnEnabled subscribe misses it — retry like
            // ExamineOverlaySubSystem → ExamineSubSystem.
            TryBindSelectionEvents();
        }

        private void TryBindSelectionEvents()
        {
            if (_selectionEventsBound && _selectionSystem == null)
            {
                _selectionEventsBound = false;
            }

            if (_selectionEventsBound)
            {
                return;
            }

            if (!SubSystems.TryGet(out _selectionSystem) || _selectionSystem == null)
            {
                return;
            }

            _selectionSystem.OnSelectableChanged += UpdateExaminable;
            _selectionEventsBound = true;

            // Catch up current hover so examine is not blank until the next selection change.
            UpdateExaminable();
        }

        private void UnsubscribeSelectionEvents()
        {
            if (_selectionSystem != null && _selectionEventsBound)
            {
                _selectionSystem.OnSelectableChanged -= UpdateExaminable;
            }

            _selectionSystem = null;
            _selectionEventsBound = false;
        }

        private void UpdateExaminable()
        {
            if (_selectionSystem == null)
            {
                return;
            }

            OnExaminableChanged?.Invoke(GetCurrentExaminable());
        }

        /// <summary>
        /// What is under the cursor as an examinable: its own <see cref="IExaminable"/>, or for an item
        /// without one (or with one that has no data) an <see cref="ItemFallbackExaminable"/>, reused while
        /// the same item stays hovered.
        /// </summary>
        public IExaminable GetCurrentExaminable()
        {
            if (_selectionSystem == null)
            {
                return null;
            }

            IExaminable current = _selectionSystem.GetCurrentSelectable<IExaminable>();
            if (current != null && current.GetData() != null)
            {
                return current;
            }

            // No examinable, or one without data (soda cans carry a bare provider): examine the item.
            Item item = _selectionSystem.GetCurrentSelectable<Item>();
            if (item == null)
            {
                return current;
            }

            if (_lastFallback == null || _lastFallback.Item != item)
            {
                _lastFallback = new ItemFallbackExaminable(item);
            }

            return _lastFallback;
        }

        /// <summary>
        /// Opens the detailed examine panel for a target chosen from the radial menu.
        /// </summary>
        public void ShowDetailedExamine(IExaminable examinable)
        {
            OnDetailedExamineRequested?.Invoke(examinable);
        }

        /// <summary>
        /// Requests the persistent character-examine window for <paramref name="examinable"/>.
        /// No-op unless something is listening (the character-examine window is character-only).
        /// </summary>
        public void RequestCharacterWindow(IExaminable examinable)
        {
            OnCharacterWindowRequested?.Invoke(examinable);
        }
    }
}
