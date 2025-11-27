using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Selection;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// The Examine SubSystem allows additional detail of items to be displayed when
    /// the cursor hovers over them. The particular information displayed is item
    /// and requirement dependant, and may take different formats.
    /// </summary>
    public class ExamineSubSystem : NetworkSubSystem
    {
        public delegate void ExaminableChangedHandler(AbstractExaminable examinable);

        public event ExaminableChangedHandler OnExaminableChanged;

        private SelectionSubSystem _selectionSubSystem;

        protected override void OnAwake()
        {
            base.OnAwake();
            _selectionSubSystem = Subsystems.Get<SelectionSubSystem>();
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            if (_selectionSubSystem)
            {
                _selectionSubSystem.OnSelectableChanged += UpdateExaminable;
            }
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            if (_selectionSubSystem)
            {
                _selectionSubSystem.OnSelectableChanged -= UpdateExaminable;
            }
        }

        private void UpdateExaminable()
        {
            // Get the examinable under the cursor
            AbstractExaminable current = _selectionSubSystem.GetCurrentSelectable<AbstractExaminable>();
            OnExaminableChanged?.Invoke(current);
        }
    }
}
