using SS3D.Rendering.URP;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Atmospherics.Visualization
{
    /// <summary>
    /// Publishes a synthetic atmos snapshot so the URP visualization can be tested without a
    /// running simulation or server. Attach to any GameObject in a scene, enter play mode, and
    /// point a camera at the world region defined below.
    /// </summary>
    public sealed class AtmosVisualizationDebugSource : MonoBehaviour
    {
        [Header("Activation")]
        [SerializeField] private bool _active = true;
        [Tooltip("Camera the effect renders on. Defaults to Camera.main.")]
        [SerializeField] private Camera _camera;
        [Tooltip("Rebuild the atlas every frame so inspector tweaks apply live.")]
        [SerializeField] private bool _rebuildEveryFrame = true;

        [Header("World region (tile coordinates)")]
        [SerializeField] private int _originX;
        [SerializeField] private int _originZ;
        [SerializeField] [Range(4, 128)] private int _size = 32;

        [Header("Synthetic gas")]
        [Tooltip("Base pressure filling the region (kPa).")]
        [SerializeField] private float _basePressure = 160f;
        [Tooltip("Peak temperature at the hot spot centre (K).")]
        [SerializeField] private float _hotSpotTemperature = 1200f;
        [SerializeField] [Range(0f, 1f)] private float _hotSpotRadius = 0.35f;
        [Tooltip("Fire intensity at the hot spot centre.")]
        [SerializeField] private float _fireIntensity = 1f;
        [SerializeField] [Range(0f, 1f)] private float _plasmaFraction = 0.6f;
        [Tooltip("Peak CO₂ mole fraction at the hot spot centre (combustion smoke).")]
        [SerializeField] [Range(0f, 1f)] private float _co2Fraction = 0.45f;

        private Texture2D _pressure;
        private Texture2D _temperature;
        private Texture2D _composition;
        private Texture2D _fire;
        private Texture2D _mask;
        private Texture2D _flow;

        private float[] _pressureData;
        private float[] _temperatureData;
        private float[] _fireData;
        private Color32[] _compositionData;
        private byte[] _maskData;
        private float[] _flowData;
        private bool _built;

        private void OnEnable()
        {
            AtmosRenderContext.SetDebugSnapshotOverride(true);
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            AtmosRenderContext.SetDebugSnapshotOverride(false);
            AtmosRenderContext.ClearRequest();
            AtmosRenderContext.ClearSnapshot();
        }

        private void OnDestroy()
        {
            DestroyTexture(ref _pressure);
            DestroyTexture(ref _temperature);
            DestroyTexture(ref _composition);
            DestroyTexture(ref _fire);
            DestroyTexture(ref _mask);
            DestroyTexture(ref _flow);
        }

        private void Update()
        {
            if (!_active)
            {
                AtmosRenderContext.ClearSnapshot();
                return;
            }

            if (!_built || _rebuildEveryFrame)
                Rebuild();

            AtmosRenderContext.SetSnapshot(BuildSnapshot());
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!_active)
                return;

            Camera target = _camera != null ? _camera : Camera.main;
            if (target == null || camera != target)
                return;

            AtmosRenderContext.SetRequest(new AtmosRenderContext.Request { SourceCamera = target });
        }

        [ContextMenu("Rebuild Synthetic Atlas")]
        public void Rebuild()
        {
            EnsureTextures();

            float half = _size * 0.5f;
            float maxRadius = half * Mathf.Max(_hotSpotRadius, 0.01f);

            for (int z = 0; z < _size; z++)
            {
                for (int x = 0; x < _size; x++)
                {
                    int i = z * _size + x;

                    _maskData[i] = 1;
                    _pressureData[i] = _basePressure;

                    float dx = x - half;
                    float dz = z - half;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);
                    float hot = Mathf.Clamp01(1f - distance / maxRadius);

                    _temperatureData[i] = Mathf.Lerp(293.15f, _hotSpotTemperature, hot);
                    _fireData[i] = _fireIntensity * hot;

                    float plasma = _plasmaFraction * hot;
                    float co2 = _co2Fraction * hot;
                    float remaining = Mathf.Max(0f, 1f - plasma - co2);
                    _compositionData[i] = new Color32(
                        ToByte(remaining * 0.5f),
                        ToByte(remaining * 0.5f),
                        ToByte(co2),
                        ToByte(plasma));
                }
            }

            WriteSyntheticFlow();

            _pressure.SetPixelData(_pressureData, 0);
            _pressure.Apply(false, false);
            _temperature.SetPixelData(_temperatureData, 0);
            _temperature.Apply(false, false);
            _fire.SetPixelData(_fireData, 0);
            _fire.Apply(false, false);
            _composition.SetPixelData(_compositionData, 0);
            _composition.Apply(false, false);
            _mask.SetPixelData(_maskData, 0);
            _mask.Apply(false, false);
            _flow.SetPixelData(_flowData, 0);
            _flow.Apply(false, false);

            _built = true;
        }

        private void WriteSyntheticFlow()
        {
            float half = _size * 0.5f;

            for (int z = 0; z < _size; z++)
            {
                for (int x = 0; x < _size; x++)
                {
                    int texel = z * _size + x;
                    int flowIndex = texel * 2;

                    float dx = x - half;
                    float dz = z - half;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);
                    if (distance <= 0.01f)
                    {
                        _flowData[flowIndex] = 0.5f;
                        _flowData[flowIndex + 1] = 0.5f;
                        continue;
                    }

                    // Radial outward flow from the hot-spot centre without altering pressure.
                    var gradient = new Vector2(dx / distance, dz / distance);
                    _flowData[flowIndex] = gradient.x * 0.5f + 0.5f;
                    _flowData[flowIndex + 1] = gradient.y * 0.5f + 0.5f;
                }
            }
        }

        private AtmosRenderContext.Snapshot BuildSnapshot()
        {
            return new AtmosRenderContext.Snapshot
            {
                Pressure = _pressure,
                Temperature = _temperature,
                Composition = _composition,
                FireIntensity = _fire,
                Flow = _flow,
                Mask = _mask,
                AtlasBounds = new Vector4(_originX, _originZ, _size, _size),
                MapId = 0,
                Valid = true,
                IgnitionTemperature = 373.15f,
            };
        }

        private void EnsureTextures()
        {
            int pixelCount = _size * _size;
            EnsureTexture(ref _pressure, TextureFormat.RFloat, FilterMode.Bilinear);
            EnsureTexture(ref _temperature, TextureFormat.RFloat, FilterMode.Bilinear);
            EnsureTexture(ref _composition, TextureFormat.RGBA32, FilterMode.Bilinear);
            EnsureTexture(ref _fire, TextureFormat.RFloat, FilterMode.Bilinear);
            EnsureTexture(ref _mask, TextureFormat.R8, FilterMode.Point);
            EnsureTexture(ref _flow, TextureFormat.RGFloat, FilterMode.Bilinear);

            EnsureArray(ref _pressureData, pixelCount);
            EnsureArray(ref _temperatureData, pixelCount);
            EnsureArray(ref _fireData, pixelCount);
            EnsureArray(ref _compositionData, pixelCount);
            EnsureArray(ref _maskData, pixelCount);
            EnsureArray(ref _flowData, pixelCount * 2);
        }

        private void EnsureTexture(ref Texture2D texture, TextureFormat format, FilterMode filterMode)
        {
            if (texture != null && texture.width == _size && texture.height == _size && texture.format == format)
                return;

            if (texture != null)
                Destroy(texture);

            texture = new Texture2D(_size, _size, format, false, true)
            {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp,
                name = $"AtmosDebug{format}",
            };
        }

        private static void EnsureArray<T>(ref T[] array, int length)
        {
            if (array == null || array.Length != length)
                array = new T[length];
        }

        private static byte ToByte(float value)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
                return;

            Destroy(texture);
            texture = null;
        }
    }
}
