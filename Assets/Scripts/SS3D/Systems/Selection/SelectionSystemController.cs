using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core.Behaviours;
using UnityEngine.UI;
using UnityEngine.Experimental.Rendering;
using System;

namespace SS3D.Systems.Selection
{
    /// <summary>
    /// The Selection System allows specific items to be selected or identified
    /// when the cursor hovers over them. It uses a shader based mesh selection
    /// methodology. The Selection System itself only identifies the object under
    /// the cursor; however, other Systems (e.g. Examine, Interaction) may make
    /// use of the results.
    /// </summary>
    public class SelectionSystemController : SS3D.Core.Behaviours.System
    {
        private SelectionSystem _system;

        public event SelectableChangedHandler OnSelectableChanged;

        public delegate void SelectableChangedHandler();

        private bool initialized = false;

        protected override void OnAwake()
        {
            base.OnAwake();
            InitializeSelectionSystem();
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            _system.OnSelectableChanged += SelectableChanged;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            _system.OnSelectableChanged -= SelectableChanged;
        }

        private void SelectableChanged()
        {
            OnSelectableChanged.Invoke();
        }

        private void InitializeSelectionSystem()
        {
            if (!initialized)
            {
                _system = new SelectionSystem();
                initialized = true;
            }
        }

        /// <summary>
        /// This method is called by individual selectables. It allocates the selectable
        /// a unique color for rendering by the Selection Camera.
        /// </summary>
        /// <param name="selectable"></param>
        /// <returns>The color that the Selectable will be rendered in.</returns>
        public Color32 RegisterSelectable(Selectable selectable)
        {
            if (!initialized) InitializeSelectionSystem();
            return _system.RegisterSelectable(selectable);
        }

        /// <summary>
        /// This method is called by the Selection Camera every frame, and simply
        /// provides the selection color immediately under the cursor.
        /// </summary>
        /// <param name="color"></param>
        public void UpdateColourFromCamera(Color32 color)
        {
            _system.UpdateColourFromCamera(color);
        }

        /// <summary>
        /// Called by systems that use the Selection System to get the selectable object
        /// in their desired type. In most instances, the selectable object will be the
        /// one stored in the _current variable.
        /// </summary>
        /// <typeparam name="T">The component type sought by the external system (e.g. IExaminable for Examine System)</typeparam>
        /// <returns>A component of type T attached to the currently hovered selectable or their nearest ancestor.</returns>
        public T GetCurrentSelectable<T>()
        {
            return _system.GetCurrentSelectable<T>();
        }
    }
}