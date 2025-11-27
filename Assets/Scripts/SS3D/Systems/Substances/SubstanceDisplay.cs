using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Substances
{
    public class SubstanceDisplay : NetworkBehaviour
    {
        private static readonly int FillAmount = Shader.PropertyToID("_FillAmount");

        private static readonly int Tint = Shader.PropertyToID("_Tint");

        private static readonly int TopColor = Shader.PropertyToID("_TopColor");

        private static readonly int WobbleX = Shader.PropertyToID("_WobbleX");

        private static readonly int WobbleZ = Shader.PropertyToID("_WobbleZ");

        /// <summary>
        /// The container to display
        /// </summary>
        [SerializeField]
        private SubstanceContainer _container;

        /// <summary>
        /// The object displaying the fluid level
        /// </summary>
        [SerializeField]
        private GameObject _displayObject;

        /// <summary>
        /// The position of fill when empty
        /// </summary>
        [SerializeField]
        private Vector3 _emptyPosition;

        /// <summary>
        /// The position of fill when full
        /// </summary>
        [SerializeField]
        private Vector3 _fullPosition;

        [SerializeField]
        private AnimationCurve _scaleX;

        [SerializeField]
        private AnimationCurve _scaleY;

        [SerializeField]
        private AnimationCurve _scaleZ;

        [SerializeField]
        private float _maxWobble = 0.03f;

        [SerializeField]
        private float _wobbleSpeed = 1f;

        [SerializeField]
        private float _recovery = 1f;

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
            _meshRenderer = _displayObject.GetComponent<Renderer>();
            if (IsServer)
            {
                _container.OnContentChanged += container => UpdateDisplay();
                UpdateDisplay();
            }
        }

        protected void Update()
        {
            _time += Time.deltaTime;

            // decrease wobble over time
            _wobbleAmountToAddX = Mathf.Lerp(_wobbleAmountToAddX, 0, Time.deltaTime * _recovery);
            _wobbleAmountToAddZ = Mathf.Lerp(_wobbleAmountToAddZ, 0, Time.deltaTime * _recovery);

            // make a sine wave of the decreasing wobble
            _pulse = 2 * Mathf.PI * _wobbleSpeed;
            _wobbleAmountX = _wobbleAmountToAddX * Mathf.Sin(_pulse * _time);
            _wobbleAmountZ = _wobbleAmountToAddZ * Mathf.Sin(_pulse * _time);

            // send it to the shader
            _meshRenderer.material.SetFloat(WobbleX, _wobbleAmountX);
            _meshRenderer.material.SetFloat(WobbleZ, _wobbleAmountZ);

            // velocity
            _velocity = (_lastPos - transform.position) / Time.deltaTime;
            _angularVelocity = transform.rotation.eulerAngles - _lastRot;

            // add clamped velocity to wobble
            _wobbleAmountToAddX += Mathf.Clamp((_velocity.x + (_angularVelocity.z * 0.2f)) * _maxWobble, -_maxWobble, _maxWobble);
            _wobbleAmountToAddZ += Mathf.Clamp((_velocity.z + (_angularVelocity.x * 0.2f)) * _maxWobble, -_maxWobble, _maxWobble);

            // keep last position
            _lastPos = transform.position;
            _lastRot = transform.rotation.eulerAngles;
        }

        [Server]
        private void UpdateDisplay()
        {
            float relativeVolume = _container.CurrentVolume / _container.Volume;
            Transform trans = _displayObject.transform;

            Color newColor = CalculateColor();

            _meshRenderer.material.SetFloat(FillAmount, relativeVolume);
            _meshRenderer.material.SetColor(Tint, newColor);
            _meshRenderer.material.SetColor(TopColor, newColor);

            // trans.localPosition = Vector3.Lerp(EmptyPosition, FullPosition, Mathf.Min(relativeVolume, 1));
            // trans.localScale = new Vector3(ScaleX.Evaluate(relativeVolume), ScaleY.Evaluate(relativeVolume), ScaleZ.Evaluate(relativeVolume));
            RpcUpdateDisplay(trans.localPosition, trans.localScale, newColor, relativeVolume);
        }

        private Color CalculateColor()
        {
            float totalMilliMoles = _container.TotalMilliMoles;
            Color color = new Color(0, 0, 0, 0);
            foreach (SubstanceEntry entry in _container.Substances)
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

            Transform trans = _displayObject.transform;
            trans.localPosition = position;
            trans.localScale = scale;
            _meshRenderer.material.SetFloat(FillAmount, relativeVolume);
            _meshRenderer.material.SetColor(Tint, color);
            _meshRenderer.material.SetColor(TopColor, color);
        }
    }
}
