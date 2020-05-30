using Mirror;
using SS3D.Engine.Substances;
using UnityEngine;

namespace SS3D.Content.Systems.Substances
{
    public class SubstanceDisplay : NetworkBehaviour
    {
        /// <summary>
        /// The container to display
        /// </summary>
        public SubstanceContainer Container;
        /// <summary>
        /// The object displaying the fluid level
        /// </summary>
        public GameObject DisplayObject;
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
            meshRenderer = DisplayObject.GetComponent<MeshRenderer>();
            if (isServer)
            {
                Container.ContentsChanged += container => UpdateDisplay();
                UpdateDisplay();
            }
        }

        [Server]
        private void UpdateDisplay()
        {
            float relativeVolume = Container.CurrentVolume / Container.Volume;
            Transform trans = DisplayObject.transform;
            trans.localPosition = Vector3.Lerp(EmptyPosition, FullPosition, Mathf.Min(relativeVolume, 1));
            trans.localScale = new Vector3(ScaleX.Evaluate(relativeVolume), ScaleY.Evaluate(relativeVolume), ScaleZ.Evaluate(relativeVolume));
            Color color = meshRenderer.material.color = CalculateColor();
            RpcUpdateDisplay(trans.localPosition, trans.localScale, color);
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

        [ClientRpc]
        private void RpcUpdateDisplay(Vector3 position, Vector3 scale, Color color)
        {
            Transform trans = DisplayObject.transform;
            trans.localPosition = position;
            trans.localScale = scale;
            meshRenderer.material.color = color;
        }
    }
}
