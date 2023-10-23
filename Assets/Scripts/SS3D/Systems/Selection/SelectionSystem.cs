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
    public class SelectionSystem
    {
        public event SelectableChangedHandler OnSelectableChanged;

        public delegate void SelectableChangedHandler();

        private Dictionary<Color32, Selectable> _selectables;
        private uint nextColour;
        private Selectable _current;

        public SelectionSystem()
        {
            GenerateSelectableDictionary();
        }

        /// <summary>
        /// This method is called by individual selectables. It allocates the selectable
        /// a unique color for rendering by the Selection Camera.
        /// </summary>
        /// <param name="selectable"></param>
        /// <returns>The color that the Selectable will be rendered in.</returns>
        public Color32 RegisterSelectable(Selectable selectable)
        {
            Color32 col = UIntToColor(nextColour);
            _selectables.Add(col, selectable);
            nextColour++;
            return col;
        }

        /// <summary>
        /// This method is called by the Selection Camera every frame, and simply
        /// provides the selection color immediately under the cursor.
        /// </summary>
        /// <param name="color"></param>
        public void UpdateColourFromCamera(Color32 color)
        {
            Selectable previous = _current;
            _selectables.TryGetValue(color, out _current);
            if (previous != _current)
            {
                OnSelectableChanged?.Invoke();
            }
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
            // See if the desired selectable type is on the currently hovered gameobject.
            GameObject go = _current.gameObject;
            T returnValue = go.GetComponent<T>();

            // If not, search ancestors until we find the type, or there are no more ancestors.
            while (returnValue == null && go.transform?.parent != null)
            {
                go = go.transform.parent.gameObject;
                returnValue = go.GetComponent<T>();
            }

            // Return the relevant object (or null)
            return returnValue;
        }

        /// <summary>
        /// This simply casts an integer index into a colour. It supports integers
        /// up to 16,777,216 (256 to the power of three), but will potentially have
        /// misreads after that amount.
        /// </summary>
        /// <param name="index">The unsigned integer to cast.</param>
        /// <returns>A unique Color32 used by the Selection Shader to render the Selectable.</returns>
        /// <exception cref="Exception">Exception raised when too many Selectables have been registered.</exception>
        private Color32 UIntToColor(uint index)
        {
            byte[] intBytes = BitConverter.GetBytes(index);
            Color32 col = new Color32(intBytes[0], intBytes[1], intBytes[2], 255);
            if (intBytes[3] > 0)
            {
                throw new Exception($"Selection System has registered too many selectables. Misreads may occur!");
            }
            return col;
        }

        /// <summary>
        /// Initializes the selectables dictionary which matches colours to selectables.
        /// Also associates the colour black with no selectable.
        /// </summary>
        private void GenerateSelectableDictionary()
        {
            _selectables = new Dictionary<Color32, Selectable>();
            nextColour = 0;
            RegisterSelectable(null);
        }

    }
}