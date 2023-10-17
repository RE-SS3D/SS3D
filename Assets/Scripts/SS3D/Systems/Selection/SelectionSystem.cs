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
    public class SelectionSystem : SS3D.Core.Behaviours.System
    {
        private Dictionary<Color32, Selectable> _selectables;
        private uint nextColour;
        private bool initialized = false;
        private Selectable _current;


        protected override void OnStart()
        {
            base.OnStart();
            GenerateSelectableDictionary();

        }

        /// <summary>
        /// This method is called by individual selectables. It allocates the selectable
        /// a unique color for rendering by the Selection Camera.
        /// </summary>
        /// <param name="selectable"></param>
        /// <returns></returns>
        public Color32 RegisterSelectable(Selectable selectable)
        {
            // Just in case Selectables are initialized before the Selection System is.
            if (!initialized)
            {
                GenerateSelectableDictionary();
            }

            // Register the selectable, and return the unique colour.
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
                Debug.Log($"Current selectable is {_current?.name}.");
            }
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
            if (initialized) return;
            initialized = true; // Set this immediately so we don't hit a stack overflow.
            _selectables = new Dictionary<Color32, Selectable>();
            nextColour = 0;
            RegisterSelectable(null);
        }

    }
}