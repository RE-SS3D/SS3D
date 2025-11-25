using SS3D.Core;
using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Selection
{
    [DisallowMultipleComponent]
    public class Selectable : Actor
    {
        private static readonly int SelectionColorPropertyId = Shader.PropertyToID("_SelectionColor");

        /// <summary>
        /// The color that this Selectable will be rendered by the Selection Camera
        /// </summary>
        public Color32 SelectionColor { get; set; }

        protected override void OnStart()
        {
            base.OnStart();
            SelectionColor = Subsystems.Get<SelectionSubSystem>().RegisterSelectable(this);
            SetColorRecursively(gameObject, SelectionColor, this);
        }

        private static void SetColorRecursively(GameObject go, Color32 color, Selectable initial)
        {
            // If the gameobject is selectable in its own right, it will set its own color
            if (go.TryGetComponent(out Selectable current) && current != initial)
            {
                return;
            }

            // If the gameobject has renderers, add SelectionColor to their MaterialPropertyBlock.;
            if (go.TryGetComponent(out Renderer renderer))
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(SelectionColorPropertyId, color);
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
