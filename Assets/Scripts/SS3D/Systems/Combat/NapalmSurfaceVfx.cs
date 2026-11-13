using System.Collections.Generic;
using Coimbra;
using FishNet;
using FishNet.Object;
using SS3D.Core;
using SS3D.Rendering.URP;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Audio;
using SS3D.Systems.Health;
using AudioType = SS3D.Systems.Audio.AudioType;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SS3D.Systems.Combat
{
    /// <summary>Client presentation of real surface deposits. No cursor-end flares or floating ember emitters.</summary>
    public sealed class NapalmSurfaceVfx : MonoBehaviour
    {
        /// <summary>Char reveals over the last seconds of fuel, so the mark is complete when the flames die.</summary>
        private const float ScorchRevealSeconds = 2.5f;
        // Walls are thin face skins on a full-cell box collider; floors need a shallow box so feet stay clean.
        private const float FloorScorchDepth = 0.18f;
        private const float WallScorchDepth = 0.45f;
        private static readonly Dictionary<int, NapalmSurfaceVfx> Views = new();
        private static Material _flameMaterial;
        private static Material _smokeMaterial;
        private static Material _scorchMaterial;
        private static AudioSource _burnVoice;
        private int _id;
        private bool _isBody;
        public bool IsBodyFire => _isBody;
        public bool IsBurning => _expires > Time.time;
        private float _expires;
        private float _credit;
        private Vector3 _normal;
        private ParticleSystem _flame;
        private ParticleSystem _smoke;
        private Light _light;
        private DecalProjector _scorch;
        private float _scorchSpin;
        private float _scorchStrength;
        private float _scorchReveal;
        /// <summary>0 while fuel burns hot, 1 once the cold char is fully visible.</summary>
        public float ScorchReveal => _scorchReveal;
        public bool HasScorch => _scorch != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Views.Clear();
            StopAudio();
            if (_flameMaterial != null) Destroy(_flameMaterial);
            if (_smokeMaterial != null) Destroy(_smokeMaterial);
            if (_scorchMaterial != null) Destroy(_scorchMaterial);
            _flameMaterial = null;
            _smokeMaterial = null;
            _scorchMaterial = null;
        }

        private static readonly HashSet<int> Retained = new();
        private static readonly List<int> Removed = new();

        public static void Sync(NapalmPatchSnapshot[] patches)
        {
            if (!MuzzleFlashVfx.IsSupported) return;
            HashSet<int> retained = Retained;
            retained.Clear();
            int count = Mathf.Min(patches.Length, NapalmPatchSet.Capacity + NapalmSubSystem.MaxBurningBodies);
            for (int i = 0; i < count; i++)
            {
                NapalmPatchSnapshot patch = patches[i];
                NetworkObject body = null;
                if (patch.IsBody)
                {
                    // The hub may be observed before this body (or outside its AOI). Resolve an id locally;
                    // never deserialize an unspawned NetworkObject reference from the buffered snapshot.
                    if (patch.Remaining <= 0f || InstanceFinder.ClientManager == null
                        || !InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(patch.BodyObjectId, out body)
                        || body == null) continue;
                }
                retained.Add(patch.Id);
                if (!Views.TryGetValue(patch.Id, out NapalmSurfaceVfx view) || view == null)
                {
                    if (!EnsureMaterials()) return;
                    view = new GameObject(patch.IsBody ? "NapalmBodyFire" : "NapalmSurfaceFire").AddComponent<NapalmSurfaceVfx>();
                    view._isBody = patch.IsBody;
                    view._id = patch.Id;
                    view._flame = view.MakeParticles("SurfaceFlame", _flameMaterial, false);
                    view._smoke = view.MakeParticles("SurfaceSmoke", _smokeMaterial, true);
                    view._light = view.gameObject.AddComponent<Light>();
                    view._light.color = new Color(1f, 0.32f, 0.04f);
                    view._light.shadows = LightShadows.None;
                    view._light.range = 1.8f;
                    if (!patch.IsBody) view.MakeScorch(patch.Normal);
                    Views[patch.Id] = view;
                }
                if (patch.IsBody)
                {
                    Transform anchor = body.transform;
                    foreach (ZoneTargetCollider zone in body.GetComponentsInChildren<ZoneTargetCollider>(true))
                    {
                        if (zone.Zone != patch.Zone) continue;
                        anchor = zone.transform;
                        break;
                    }
                    view.transform.SetParent(anchor, false);
                    view.transform.localPosition = anchor == body.transform ? Vector3.up * 0.8f : Vector3.zero;
                    view.transform.localRotation = Quaternion.identity;
                    view._normal = Vector3.up;
                }
                else
                {
                    view._normal = patch.Normal.normalized;
                    view.transform.SetPositionAndRotation(patch.Point + view._normal * 0.015f,
                        Quaternion.FromToRotation(Vector3.forward, view._normal));
                    if (view._scorch != null)
                        view._scorch.transform.rotation = BloodDecalSpawner.RotationOntoSurface(view._normal, view._scorchSpin);
                }
                view._expires = Time.time + patch.Remaining;
                view.enabled = true;
                if (patch.Remaining > 0f && !view._flame.isPlaying)
                {
                    view._flame.Play();
                    view._smoke.Play();
                }
            }
            List<int> removed = Removed;
            removed.Clear();
            foreach (KeyValuePair<int, NapalmSurfaceVfx> pair in Views)
                if (!retained.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (int id in removed)
            {
                NapalmSurfaceVfx view = Views[id];
                Views.Remove(id);
                if (view != null) view.gameObject.Dispose(true);
            }
            UpdateAudio();
        }

        public static void Clear()
        {
            foreach (NapalmSurfaceVfx view in Views.Values)
                if (view != null) view.gameObject.Dispose(true);
            Views.Clear();
            StopAudio();
        }

        private static void UpdateAudio()
        {
            if (!AudioSourceOcclusion.TryGetListenerPosition(out Vector3 listener)) { StopAudio(); return; }
            NapalmSurfaceVfx nearest = null;
            float best = 225f;
            foreach (NapalmSurfaceVfx view in Views.Values)
            {
                if (view == null || !view.IsBurning) continue;
                float distance = (view.transform.position - listener).sqrMagnitude;
                if (distance < best) { best = distance; nearest = view; }
            }
            if (nearest == null) { StopAudio(); return; }
            if (_burnVoice == null && SubSystems.TryGet(out AudioSubSystem audio))
                _burnVoice = audio.PlayAudioSourceLocal(AudioType.Sfx, CombatAudioTrackIds.FlameJet,
                    nearest.transform.position, null, true, 0.35f, 0.9f, 1f, 15f);
            if (_burnVoice != null) _burnVoice.transform.position = nearest.transform.position;
        }

        private static void StopAudio()
        {
            if (_burnVoice != null) { _burnVoice.Stop(); _burnVoice.loop = false; _burnVoice = null; }
        }

        private static bool EnsureMaterials()
        {
            if (_flameMaterial != null) return true;
            Shader shader = Resources.Load<Shader>("Combat/Flame");
            if (shader == null) return false;
            _flameMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _flameMaterial.SetFloat("_Intensity", 2.3f);
            _flameMaterial.SetFloat("_SurfaceFlame", 1f);
            _smokeMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _smokeMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            return true;
        }

        /// <summary>Same URP decal path as blood and blast scorches, with a noisier napalm char mask.</summary>
        private static Material ScorchMaterial()
        {
            if (_scorchMaterial != null) return _scorchMaterial;
            BlastVfxCatalog catalog = BlastVfxCatalog.Instance;
            if (catalog == null || catalog.ScorchDecalMaterial == null) return null;
            _scorchMaterial = new Material(catalog.ScorchDecalMaterial) { name = "NapalmScorch", hideFlags = HideFlags.HideAndDontSave };
            Texture2D mask = Resources.Load<Texture2D>("Combat/NapalmScorch");
            if (mask != null)
            {
                if (_scorchMaterial.HasProperty("Base_Map")) _scorchMaterial.SetTexture("Base_Map", mask);
                if (_scorchMaterial.HasProperty("_BaseMap")) _scorchMaterial.SetTexture("_BaseMap", mask);
            }
            return _scorchMaterial;
        }

        private void MakeScorch(Vector3 normal)
        {
            Material material = ScorchMaterial();
            if (material == null) return;
            GameObject child = new("NapalmScorchDecal");
            child.transform.SetParent(transform, false);
            _scorch = child.AddComponent<DecalProjector>();
            // Mask first, then a property that calls OnValidate (see BloodDecalSpawner).
            _scorch.renderingLayerMask = DecalRenderingLayers.WorldFloorProjectorMask;
            _scorch.material = material;
            _scorch.scaleMode = DecalScaleMode.ScaleInvariant;
            bool wall = Mathf.Abs(Vector3.Dot(normal.normalized, Vector3.up)) < 0.5f;
            float depth = wall ? WallScorchDepth : FloorScorchDepth;
            float size = NapalmPatchSet.Radius * 2f * Random.Range(1.05f, 1.4f);
            _scorch.size = new Vector3(size * Random.Range(0.8f, 1.2f), size * Random.Range(0.8f, 1.2f), depth);
            _scorch.pivot = new Vector3(0f, 0f, depth * 0.5f);
            _scorch.startAngleFade = 180f;
            _scorch.endAngleFade = 180f;
            _scorch.drawDistance = 40f;
            _scorch.fadeFactor = 0f;
            _scorchSpin = Random.Range(0f, 360f);
            _scorchStrength = Random.Range(0.75f, 0.95f);
            child.transform.rotation = BloodDecalSpawner.RotationOntoSurface(normal, _scorchSpin);
        }

        private ParticleSystem MakeParticles(string name, Material material, bool smoke)
        {
            GameObject child = new(name);
            child.transform.SetParent(transform, false);
            ParticleSystem system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = smoke ? 24 : 64;
            main.startRotation = smoke ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f) : new ParticleSystem.MinMaxCurve(0f);
            main.startSize3D = !smoke;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;
            Gradient gradient = new();
            gradient.SetKeys(smoke
                ? new[] { new GradientColorKey(new Color(0.14f, 0.12f, 0.1f), 0f), new GradientColorKey(new Color(0.06f, 0.06f, 0.06f), 1f) }
                : new[] { new GradientColorKey(new Color(1f, 0.8f, 0.12f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.015f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(smoke ? 0.1f : 0.85f, 0.15f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            color.color = gradient;
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, smoke ? 0.6f : 0.9f, 1f, smoke ? 1.8f : 0.5f));
            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            if (!smoke) renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            system.Play();
            return system;
        }

        private void Update() => Tick(Time.time, Time.deltaTime);

        private void Tick(float now, float deltaTime)
        {
            float remaining = _expires - now;
            if (_scorch != null)
            {
                // Never un-char a surface when it is sprayed again.
                float reveal = Mathf.InverseLerp(ScorchRevealSeconds, -0.5f, remaining);
                _scorchReveal = Mathf.Max(_scorchReveal, reveal * reveal * (3f - 2f * reveal));
                _scorch.fadeFactor = _scorchStrength * _scorchReveal;
            }
            if (remaining <= -1f)
            {
                _light.intensity = 0f;
                _credit = 0f;
                if (_flame.isPlaying) _flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (_smoke.isPlaying) _smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (_isBody)
                {
                    Views.Remove(_id);
                    if (Views.Count == 0) StopAudio();
                    gameObject.Dispose(true);
                    return;
                }
                // Cold scorch: nothing left to animate until a new snapshot re-ignites it.
                if (_scorchReveal >= 1f) enabled = false;
                return;
            }
            float fade = Mathf.Clamp01(remaining);
            _light.intensity = 0.8f * fade;
            _credit += Mathf.Min(deltaTime, 0.05f) * 60f * fade;
            int count = Mathf.FloorToInt(_credit);
            _credit -= count;
            for (int i = 0; i < count; i++)
            {
                Vector2 disk = Random.insideUnitCircle * (NapalmPatchSet.Radius * 0.85f);
                Vector3 point = _isBody
                    ? transform.position + Random.insideUnitSphere * 0.2f + Vector3.up * Random.Range(0f, 0.35f)
                    : transform.TransformPoint(new Vector3(disk.x, disk.y, 0f));
                ParticleSystem.EmitParams flame = new()
                {
                    position = point, velocity = Vector3.up * Random.Range(0.55f, 0.9f) + _normal * 0.12f,
                    startLifetime = Random.Range(0.5f, 0.85f),
                    startSize3D = new Vector3(Random.Range(0.65f, 0.95f), Random.Range(0.8f, 1.3f), 1f), startColor = Color.white,
                };
                _flame.Emit(flame, 1);
                if (Random.value < 0.1f)
                {
                    flame.velocity = Vector3.up * 0.5f;
                    flame.startLifetime = 1f;
                    flame.startSize = 0.55f;
                    _smoke.Emit(flame, 1);
                }
            }
        }
    }
}
