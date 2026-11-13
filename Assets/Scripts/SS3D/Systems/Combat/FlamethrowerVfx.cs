using System.Collections.Generic;
using Coimbra;
using SS3D.Core;
using SS3D.Systems.Audio;
using UnityEngine;
using UnityEngine.Rendering;
using AudioType = SS3D.Systems.Audio.AudioType;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// One bounded client-local jet per live muzzle. Server pulses refresh the jet; missing pulses let it
    /// expire, so release, disconnect, death and dropped weapons cannot leave a fire or audio loop running.
    /// Particles are in world space and never apply damage. The endpoint is the server's resolved contact.
    /// </summary>
    public sealed class FlamethrowerVfx : MonoBehaviour
    {
        private const int Capacity = 12;
        private const float HoldSeconds = 0.32f;
        private const float JetSpeed = 12f;
        private const float EmissionRate = 420f;
        private static readonly List<FlamethrowerVfx> Active = new();
        private static Material _fireMaterial;
        private static Material _smokeMaterial;
        private Transform _muzzle;
        private Vector3 _origin;
        private float _lastPulse;
        private Vector3 _endpoint;
        private Vector3 _previousOrigin;
        private Vector3 _direction;
        private bool _hasOrigin;
        private float _emissionCredit;
        private ParticleSystem _fire;
        private Light _light;
        private AudioSource _voice;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Active.Clear();
            if (_fireMaterial != null) Destroy(_fireMaterial);
            if (_smokeMaterial != null) Destroy(_smokeMaterial);
            _fireMaterial = null;
            _smokeMaterial = null;
        }

        public static void Play(Transform muzzle, Vector3 origin, Vector3 endpoint, bool hit, Vector3 normal)
        {
            if (!MuzzleFlashVfx.IsSupported) return;
            FlamethrowerVfx effect = null;
            foreach (FlamethrowerVfx candidate in Active)
                if (candidate != null && muzzle != null && candidate._muzzle == muzzle) { effect = candidate; break; }
            if (effect == null)
            {
                if (_fireMaterial == null)
                {
                    Shader shader = Resources.Load<Shader>("Combat/Flame");
                    if (shader == null) return;
                    _fireMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                    _fireMaterial.SetFloat("_Intensity", 2.5f);
                    _smokeMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                    _smokeMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                }
                if (Active.Count >= Capacity)
                {
                    FlamethrowerVfx oldest = Active[0];
                    Active.RemoveAt(0);
                    if (oldest != null) { oldest.StopVoice(); oldest.gameObject.SetActive(false); oldest.gameObject.Dispose(true); }
                }
                effect = new GameObject("FlamethrowerJet").AddComponent<FlamethrowerVfx>();
                effect._muzzle = muzzle;
                effect._fire = effect.MakeParticles("Flame", _fireMaterial, 256, false);
                effect._light = effect.gameObject.AddComponent<Light>();
                effect._light.color = new Color(1f, 0.32f, 0.05f);
                effect._light.range = 3f;
                effect._light.shadows = LightShadows.None;
                Active.Add(effect);
            }
            effect.Pulse(origin, endpoint, hit, normal);
        }

        private ParticleSystem MakeParticles(string name, Material material, int capacity, bool smoke)
        {
            GameObject child = new(name);
            child.transform.SetParent(transform, false);
            ParticleSystem particles = child.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = capacity;
            main.startSpeed = 0f;
            main.gravityModifier = smoke ? -0.025f : 0.12f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(smoke
                ? new[] { new GradientColorKey(new Color(0.14f, 0.12f, 0.1f), 0f), new GradientColorKey(new Color(0.04f, 0.04f, 0.04f), 1f) }
                : new[] { new GradientColorKey(new Color(1f, 0.9f, 0.25f), 0f), new GradientColorKey(new Color(1f, 0.6f, 0.06f), 0.65f), new GradientColorKey(new Color(0.5f, 0.08f, 0f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(smoke ? 0.24f : 0.65f, 0.12f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.18f, 1f, 1.35f));
            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 1f;
            if (!smoke)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.025f;
                renderer.lengthScale = 1.4f;
                ParticleSystem.CollisionModule collision = particles.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.mode = ParticleSystemCollisionMode.Collision3D;
                collision.collidesWith = LayerMask.GetMask("Default", "Walls");
                collision.enableDynamicColliders = false;
                collision.lifetimeLoss = 1f;
                collision.quality = ParticleSystemCollisionQuality.Medium;
            }
            particles.Play();
            return particles;
        }

        private void Pulse(Vector3 origin, Vector3 endpoint, bool hit, Vector3 normal)
        {
            _lastPulse = Time.time;
            _origin = _muzzle != null ? _muzzle.position : origin;
            transform.position = _origin;
            _endpoint = endpoint;
            if (!_hasOrigin)
            {
                _previousOrigin = _origin;
                _direction = (endpoint - _origin).normalized;
                _hasOrigin = true;
            }
            if (_voice == null && SubSystems.TryGet(out AudioSubSystem audio))
                _voice = audio.PlayAudioSourceLocal(AudioType.Sfx, CombatAudioTrackIds.FlameJet, _origin, null, true, 0.8f, 1f, 1.5f, 18f);
        }

        private static void Emit(ParticleSystem particles, Vector3 position, Vector3 velocity, float size, float life)
        {
            ParticleSystem.EmitParams particle = new ParticleSystem.EmitParams
            {
                position = position, velocity = velocity, startSize = size, startLifetime = life, startColor = Color.white,
            };
            particles.Emit(particle, 1);
        }

        private void LateUpdate() => Tick(Time.time, Time.deltaTime);

        // Damage RPCs only refresh the target and a short lease. Rendering has its own per-frame clock:
        // emitting a whole line on each RPC makes every fuel tick visible as a separate shot.
        private void Tick(float now, float deltaTime)
        {
            float age = now - _lastPulse;
            bool firing = age < HoldSeconds && _muzzle != null;
            Vector3 origin = _muzzle != null ? _muzzle.position : _origin;
            transform.position = origin;
            _light.intensity = firing ? 2.7f + Mathf.Sin(now * 31f) * 0.15f : 0f;
            if (firing)
            {
                float dt = Mathf.Min(deltaTime, 0.05f);
                Vector3 delta = _endpoint - origin;
                float distance = NapalmSprayRules.Range;
                Vector3 targetDirection = delta.sqrMagnitude > 0.001f ? delta.normalized : _direction;
                _direction = Vector3.Slerp(_direction, targetDirection, 1f - Mathf.Exp(-dt * 25f));
                Quaternion basis = Quaternion.LookRotation(_direction);
                _emissionCredit += dt * EmissionRate;
                int count = Mathf.FloorToInt(_emissionCredit);
                _emissionCredit -= count;
                float travelSeconds = Mathf.Max(0.015f, distance / JetSpeed);
                for (int i = 0; i < count; i++)
                {
                    // Spread births through the frame and along the moving nozzle. Once emitted, fuel
                    // keeps its world-space trajectory instead of pivoting with the weapon like a beam.
                    float fraction = (i + 0.5f) / count;
                    Vector2 disk = Random.insideUnitCircle * 0.018f;
                    Vector3 point = Vector3.Lerp(_previousOrigin, origin, fraction)
                        + basis * new Vector3(disk.x, disk.y, 0f);
                    float yaw = Random.Range(-NapalmSprayRules.HalfAngle, NapalmSprayRules.HalfAngle);
                    float pitch = Random.Range(-NapalmSprayRules.VerticalHalfAngle, NapalmSprayRules.VerticalHalfAngle);
                    Vector3 velocity = basis * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * JetSpeed;
                    float birthAge = Mathf.Min(dt * (1f - fraction), travelSeconds * 0.5f);
                    point += velocity * birthAge;
                    Emit(_fire, point, velocity, Random.Range(0.5f, 0.75f), travelSeconds - birthAge);
                }
                if (_voice != null) _voice.transform.position = origin;
            }
            else
            {
                _emissionCredit = 0f;
                StopVoice();
            }
            _previousOrigin = origin;
            if (age > 1.1f) gameObject.Dispose(true);
        }

        private void StopVoice()
        {
            if (_voice != null) { _voice.Stop(); _voice.loop = false; _voice = null; }
        }

        private void OnDestroy() { StopVoice(); Active.Remove(this); }
    }
}
