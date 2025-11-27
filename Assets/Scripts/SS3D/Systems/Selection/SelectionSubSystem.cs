using JetBrains.Annotations;
using UnityEngine;

namespace SS3D.Systems.Selection
{
    /// <summary>
    /// The Selection SubSystem allows specific items to be selected or identified
    /// when the cursor hovers over them. It uses a shader based mesh selection
    /// methodology. The Selection SubSystem itself only identifies the object under
    /// the cursor; however, other Systems (e.g. Examine, Interaction) may make
    /// use of the results.
    /// </summary>
    public class SelectionSubSystem : SS3D.Core.Behaviours.SubSystem
    {
        public delegate void SelectableChangedHandler();

        public event SelectableChangedHandler OnSelectableChanged;

        private SelectionController _controller;

        private bool _initialized;

        /// <summary>
        /// This method is called by individual selectables. It allocates the selectable
        /// a unique color for rendering by the Selection Camera.
        /// </summary>
        /// <param name="selectable"></param>
        /// <returns>The color that the Selectable will be rendered in.</returns>
        public Color32 RegisterSelectable(Selectable selectable)
        {
            if (!_initialized)
            {
                InitializeSelectionSystem();
            }

            return _controller.RegisterSelectable(selectable);
        }

        /// <summary>
        /// This method is called by the Selection Camera every frame, and simply
        /// provides the selection color immediately under the cursor.
        /// </summary>
        /// <param name="color"></param>
        public void UpdateColourFromCamera(Color32 color)
        {
            _controller.UpdateColourFromCamera(color);
        }

        /// <summary>
        /// Called by systems that use the Selection SubSystem to get the selectable object
        /// in their desired type. In most instances, the selectable object will be the
        /// one stored in the _current variable.
        /// </summary>
        /// <typeparam name="T">The component type sought by the external system (e.g. IExaminable for Examine SubSystem)</typeparam>
        /// <returns>A component of type T attached to the currently hovered selectable or their nearest ancestor.</returns>
        [CanBeNull]
        public T GetCurrentSelectable<T>()
            where T : Component => _controller.GetCurrentSelectable<T>();

        protected override void OnAwake()
        {
            base.OnAwake();
            InitializeSelectionSystem();
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            _controller.OnSelectableChanged += SelectableChanged;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            _controller.OnSelectableChanged -= SelectableChanged;
        }

        private void SelectableChanged()
        {
            OnSelectableChanged?.Invoke();
        }

        private void InitializeSelectionSystem()
        {
            if (!_initialized)
            {
                _controller = new SelectionController();
                _initialized = true;
            }
        }
    }
}
