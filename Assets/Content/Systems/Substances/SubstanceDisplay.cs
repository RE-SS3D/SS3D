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

        // wobble shader stuff
        Vector3 lastPos;
        Vector3 velocity;
        Vector3 lastRot;
        Vector3 angularVelocity;
        public float MaxWobble = 0.03f;
        public float WobbleSpeed = 1f;
        public float Recovery = 1f;
        float wobbleAmountX;
        float wobbleAmountZ;
        float wobbleAmountToAddX;
        float wobbleAmountToAddZ;
        float pulse;
        float time = 0.5f;

        private void Start()
        {
            meshRenderer = DisplayObject.GetComponent<MeshRenderer>();
            if (isServer)
            {
                Container.ContentsChanged += container => UpdateDisplay();
                UpdateDisplay();
            }
        }

        private void Update()
        {
            time += Time.deltaTime;
            // decrease wobble over time
            wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * (Recovery));
            wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * (Recovery));

            // make a sine wave of the decreasing wobble
            pulse = 2 * Mathf.PI * WobbleSpeed;
            wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
            wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);

            // send it to the shader
            meshRenderer.material.SetFloat("_WobbleX", wobbleAmountX);
            meshRenderer.material.SetFloat("_WobbleZ", wobbleAmountZ);

            // velocity
            velocity = (lastPos - transform.position) / Time.deltaTime;
            angularVelocity = transform.rotation.eulerAngles - lastRot;


            // add clamped velocity to wobble
            wobbleAmountToAddX += Mathf.Clamp((velocity.x + (angularVelocity.z * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);
            wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (angularVelocity.x * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);

            // keep last position
            lastPos = transform.position;
            lastRot = transform.rotation.eulerAngles;
        }

        [Server]
        private void UpdateDisplay()
        {
            float relativeVolume = (Container.CurrentVolume / Container.Volume);
            Transform trans = DisplayObject.transform;

            Color newColor = CalculateColor();

            meshRenderer.material.SetFloat("_FillAmount", relativeVolume);
            meshRenderer.material.SetColor("_Tint", newColor);
            meshRenderer.material.SetColor("_TopColor", newColor);

            //trans.localPosition = Vector3.Lerp(EmptyPosition, FullPosition, Mathf.Min(relativeVolume, 1));
            //trans.localScale = new Vector3(ScaleX.Evaluate(relativeVolume), ScaleY.Evaluate(relativeVolume), ScaleZ.Evaluate(relativeVolume));
            RpcUpdateDisplay(trans.localPosition, trans.localScale, newColor);
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

            color.a = 0.5f;
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
