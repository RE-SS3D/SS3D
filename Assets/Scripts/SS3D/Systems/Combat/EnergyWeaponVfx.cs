using Coimbra;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Combat
{
    public enum RangedVisualStyle : byte
    {
        Ballistic = 0,
        Laser = 1,
        Ion = 2,
        Taser = 3,
    }

    /// <summary>Bounded, client-only shot presentation. Endpoints come from the authoritative hitscan.</summary>
    public sealed class EnergyWeaponVfx : MonoBehaviour
    {
        private const int Capacity = 24;
        private const int Segments = 48;
        private static readonly List<EnergyWeaponVfx> Active = new();
        private static Material _material;

        /// <summary>Local look-development override; never changes damage or server weapon state.</summary>
        public static RangedVisualStyle? PreviewStyle { get; set; }

        private Transform _muzzle;
        private Vector3 _origin;
        private Vector3 _end;
        private Vector3 _normal;
        private RangedVisualStyle _style;
        private bool _hit;
        private float _age;
        private float _lifetime;
        private Color _color;
        private LineRenderer _halo;
        private LineRenderer _core;
        private LineRenderer _ring;
        private LineRenderer _branch;
        private Light _flash;
        private readonly Vector3[] _points = new Vector3[Segments + 1];
        private readonly Vector3[] _ringPoints = new Vector3[33];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Active.Clear();
            PreviewStyle = null;
            if (_material != null) Destroy(_material);
            _material = null;
        }

        public static void Play(RangedVisualStyle style, Transform muzzle, Vector3 origin,
            Vector3 endpoint, bool hasImpact, Vector3 impactNormal)
        {
            if (style == RangedVisualStyle.Ballistic || !MuzzleFlashVfx.IsSupported) return;
            if (_material == null)
            {
                Shader shader = Resources.Load<Shader>("Combat/EnergyWeapon");
                if (shader == null) return;
                _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            if (Active.Count >= Capacity)
            {
                EnergyWeaponVfx oldest = Active[0];
                Active.RemoveAt(0);
                if (oldest != null)
                {
                    oldest.gameObject.SetActive(false);
                    oldest.gameObject.Dispose(true);
                }
            }

            EnergyWeaponVfx effect = new GameObject($"EnergyShot_{style}").AddComponent<EnergyWeaponVfx>();
            effect._style = style;
            effect._muzzle = muzzle;
            effect._origin = origin;
            effect._end = endpoint;
            effect._hit = hasImpact;
            effect._normal = impactNormal.sqrMagnitude > 0.001f ? impactNormal.normalized : Vector3.up;
            effect._lifetime = style == RangedVisualStyle.Laser ? 0.16f : style == RangedVisualStyle.Ion ? 0.32f : 0.24f;
            effect._color = style == RangedVisualStyle.Laser ? new Color(3f, 0.08f, 0.03f)
                : style == RangedVisualStyle.Ion ? new Color(0.05f, 1.8f, 3f) : new Color(0.6f, 0.8f, 3f);
            effect._halo = effect.MakeLine("Glow", Segments + 1);
            effect._core = effect.MakeLine("Core", Segments + 1);
            effect._ring = effect.MakeLine("Impact", 33);
            effect._ring.enabled = hasImpact;
            effect._branch = effect.MakeLine("ArcBranch", 9);
            effect._branch.enabled = style == RangedVisualStyle.Taser;
            effect._flash = effect.gameObject.AddComponent<Light>();
            effect._flash.color = effect._color / 3f;
            effect._flash.range = 1.5f;
            effect._flash.shadows = LightShadows.None;
            Active.Add(effect);
            effect.Draw();
        }

        private LineRenderer MakeLine(string name, int count)
        {
            GameObject host = new(name);
            host.transform.SetParent(transform, false);
            LineRenderer line = host.AddComponent<LineRenderer>();
            line.sharedMaterial = _material;
            line.useWorldSpace = true;
            line.positionCount = count;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _lifetime) { gameObject.Dispose(true); return; }
            Draw();
        }

        private void Draw()
        {
            Vector3 start = _muzzle != null ? _muzzle.position : _origin;
            Vector3 delta = _end - start;
            Vector3 forward = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(forward, Mathf.Abs(forward.y) > 0.95f ? Vector3.right : Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, forward);
            float progress = Mathf.Clamp01(_age / _lifetime);
            float fade = (1f - progress) * (1f - progress);
            int tick = Mathf.FloorToInt(_age * 40f);
            for (int i = 0; i <= Segments; i++)
            {
                float t = i / (float)Segments;
                Vector3 point = Vector3.Lerp(start, _end, t);
                float envelope = Mathf.Sin(t * Mathf.PI);
                if (_style == RangedVisualStyle.Ion)
                {
                    float phase = t * Mathf.PI * 20f - _age * 55f;
                    point += (right * Mathf.Cos(phase) + up * Mathf.Sin(phase)) * (0.055f * envelope);
                }
                else if (_style == RangedVisualStyle.Taser && i > 0 && i < Segments)
                {
                    // Deterministic local noise; do not perturb Unity's gameplay random state.
                    float x = Mathf.PerlinNoise(i * 7.13f, tick * 2.31f) * 2f - 1f;
                    float y = Mathf.PerlinNoise(i * 3.77f, tick * 5.17f + 10f) * 2f - 1f;
                    point += (right * x + up * y) * (0.16f * envelope);
                }
                _points[i] = point;
            }
            _halo.SetPositions(_points);
            _core.SetPositions(_points);
            SetAppearance(_halo, _style == RangedVisualStyle.Laser ? 0.12f : 0.2f, _color, fade * 0.45f);
            SetAppearance(_core, _style == RangedVisualStyle.Taser ? 0.018f : 0.025f,
                Color.Lerp(_color, Color.white * 3f, 0.8f), fade);

            if (_style == RangedVisualStyle.Taser)
            {
                Vector3 fork = _points[Segments * 2 / 3];
                for (int i = 0; i < 9; i++)
                {
                    float t = i / 8f;
                    Vector3 point = Vector3.Lerp(fork, _end, t)
                        + right * (Mathf.Sin(t * Mathf.PI) * 0.24f)
                        + up * (Mathf.Sin(i * 13f + tick) * Mathf.Sin(t * Mathf.PI) * 0.09f);
                    _branch.SetPosition(i, point);
                }
                SetAppearance(_branch, 0.018f, _color, fade * 0.8f);
            }

            // Expanding surface-oriented corona, only for an actual contact (never on a miss).
            Vector3 tangent = Vector3.Cross(_normal, Mathf.Abs(_normal.y) > 0.95f ? Vector3.right : Vector3.up).normalized;
            Vector3 bitangent = Vector3.Cross(_normal, tangent);
            float radius = Mathf.Lerp(0.035f, _style == RangedVisualStyle.Ion ? 0.4f : 0.2f, progress);
            for (int i = 0; i < _ringPoints.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / (_ringPoints.Length - 1);
                _ringPoints[i] = _end + _normal * 0.015f
                    + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * radius;
            }
            _ring.SetPositions(_ringPoints);
            SetAppearance(_ring, 0.045f * (1f - progress), _color, fade);
            transform.position = start;
            _flash.intensity = 4f * fade;
        }

        private static void SetAppearance(LineRenderer line, float width, Color color, float alpha)
        {
            line.widthMultiplier = width;
            color.a = alpha;
            line.startColor = color;
            line.endColor = color;
        }

        private void OnDestroy() => Active.Remove(this);
    }
}
