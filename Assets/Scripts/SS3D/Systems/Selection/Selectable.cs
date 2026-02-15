using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Core.Behaviours;
using SS3D.Core;

namespace SS3D.Systems.Selection {

    [DisallowMultipleComponent]
    public class Selectable : Actor
    {
        /// <summary>
        /// The color that this Selectable will be rendered by the Selection Camera
        /// </summary>
        Color32 _selectionColor;

        /// <summary>
        /// The color that this Selectable will be rendered by the Selection Camera
        /// </summary>
        public Color32 SelectionColor
        {
            get => _selectionColor;
            set => _selectionColor = value;
        }

        protected override void OnStart()
        {
            base.OnStart();
            _selectionColor = SubSystems.Get<SelectionSubSystem>().RegisterSelectable(this);
            SetColorRecursively(this.gameObject, _selectionColor, this);
            
        }

        private void SetColorRecursively(GameObject go, Color32 color, Selectable initial)
        {
            // If the gameobject is selectable in its own right, it will set its own color
            Selectable current = go.GetComponent<Selectable>();
            if (current != null && current != initial) return;

            // If the gameobject has renderers, add SelectionColor to their MaterialPropertyBlock.
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_SelectionColor", color);
                renderer.SetPropertyBlock(propertyBlock);
            }

            // Call this for all children
            foreach (Transform child in go.transform)
            {
                SetColorRecursively(child.gameObject, color, initial);
            }

        }
    }
}