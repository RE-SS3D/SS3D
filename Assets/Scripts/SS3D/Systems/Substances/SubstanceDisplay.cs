using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace SS3D.Substances
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

        public float MaxWobble = 0.03f;
        public float WobbleSpeed = 1f;
        public float Recovery = 1f;

        private Renderer _meshRenderer;

        // wobble shader stuff
        private Vector3 _lastPos;
        private Vector3 _velocity;
        private Vector3 _lastRot;
        private Vector3 _angularVelocity;
        private float _wobbleAmountX;
        private float _wobbleAmountZ;
        private float _wobbleAmountToAddX;
        private float _wobbleAmountToAddZ;
        private float _pulse;
        private float _time = 0.5f;

        protected void Start()
        {
            _meshRenderer = DisplayObject.GetComponent<Renderer>();

            if (IsServer)
            {
                Container.OnContentsChanged += container => UpdateDisplay();
                UpdateDisplay();
            }
        }

        protected void Update()
        {
            _time += Time.deltaTime;

            // decrease wobble over time
            _wobbleAmountToAddX = Mathf.Lerp(_wobbleAmountToAddX, 0, Time.deltaTime * Recovery);
            _wobbleAmountToAddZ = Mathf.Lerp(_wobbleAmountToAddZ, 0, Time.deltaTime * Recovery);

            // make a sine wave of the decreasing wobble
            _pulse = 2 * Mathf.PI * WobbleSpeed;
            _wobbleAmountX = _wobbleAmountToAddX * Mathf.Sin(_pulse * _time);
            _wobbleAmountZ = _wobbleAmountToAddZ * Mathf.Sin(_pulse * _time);

            // send it to the shader
            _meshRenderer.material.SetFloat("_WobbleX", _wobbleAmountX);
            _meshRenderer.material.SetFloat("_WobbleZ", _wobbleAmountZ);

            // velocity
            _velocity = (_lastPos - transform.position) / Time.deltaTime;
            _angularVelocity = transform.rotation.eulerAngles - _lastRot;

            // add clamped velocity to wobble
            _wobbleAmountToAddX += Mathf.Clamp((_velocity.x + (_angularVelocity.z * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);
            _wobbleAmountToAddZ += Mathf.Clamp((_velocity.z + (_angularVelocity.x * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);

            // keep last position
            _lastPos = transform.position;
            _lastRot = transform.rotation.eulerAngles;
        }

        [Server]
        private void UpdateDisplay()
        {
            float relativeVolume = Container.CurrentVolume / Container.Volume;
            Transform trans = DisplayObject.transform;

            Color newColor = CalculateColor();

            _meshRenderer.material.SetFloat("_FillAmount", relativeVolume);
            _meshRenderer.material.SetColor("_Tint", newColor);
            _meshRenderer.material.SetColor("_TopColor", newColor);

            RpcUpdateDisplay(trans.localPosition, trans.localScale, newColor, relativeVolume);
        }

        private Color CalculateColor()
        {
            float totalMilliMoles = Container.TotalMilliMoles;
            Color color = new Color(0, 0, 0, 0);

            foreach (SubstanceEntry entry in Container.Substances)
            {
                float relativeMoles = entry.MilliMoles / totalMilliMoles;
                color += entry.Substance.Color * relativeMoles;
            }

            color.a = 0.5f;

            return color;
        }

        [ObserversRpc]
        private void RpcUpdateDisplay(Vector3 position, Vector3 scale, Color color, float relativeVolume)
        {
            // Ensure this is initialised.
            if (_meshRenderer == null)
            {
                Start();
            }

            Transform trans = DisplayObject.transform;
            trans.localPosition = position;
            trans.localScale = scale;
            _meshRenderer.material.SetFloat("_FillAmount", relativeVolume);
            _meshRenderer.material.SetColor("_Tint", color);
            _meshRenderer.material.SetColor("_TopColor", color);
        }
    }
}
