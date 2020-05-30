using SS3D.Engine.Substances;
using UnityEngine;

namespace SS3D.Content.Systems.Substances
{
    [RequireComponent(typeof(MeshRenderer))]
    public class SubstanceDisplay : MonoBehaviour
    {
        /// <summary>
        /// The container to display
        /// </summary>
        public SubstanceContainer Container;
        /// <summary>
        /// The position of fill when empty
        /// </summary>
        public Vector3 EmptyPosition;
        /// <summary>
        /// The position of fill when full
        /// </summary>
        public Vector3 FullPosition;
        public AnimationCurve ScaleX;
        public AnimationCurve ScaleY;
        public AnimationCurve ScaleZ;
        private MeshRenderer meshRenderer;

        private void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            Container.ContentsChanged += container => UpdateDisplay();
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            float relativeVolume = Container.CurrentVolume / Container.Volume;
            transform.localPosition = Vector3.Lerp(EmptyPosition, FullPosition, Mathf.Min(relativeVolume, 1));
            transform.localScale = new Vector3(ScaleX.Evaluate(relativeVolume), ScaleY.Evaluate(relativeVolume), ScaleZ.Evaluate(relativeVolume));
            meshRenderer.material.color = CalculateColor();
        }

        private Color CalculateColor()
        {
            float totalMoles = Container.TotalMoles;
            Color color = new Color(0, 0, 0, 0);
            foreach (SubstanceEntry entry in Container.Substances)
            {
                float relativeMoles = entry.Moles / totalMoles;
                color += entry.Substance.Color * relativeMoles;
            }

            return color;
        }
    }
}
