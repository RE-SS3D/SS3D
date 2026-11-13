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
        private static Material _emberMaterial;
        private static Material _heatMaterial;
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
        private ParticleSystem _embers;
        private ParticleSystem _heat;
        private float _heatCredit;
        private float _emberCredit;
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
            if (_emberMaterial != null) Destroy(_emberMaterial);
            if (_heatMaterial != null) Destroy(_heatMaterial);
            if (_scorchMaterial != null) Destroy(_scorchMaterial);
            _flameMaterial = null;
            _smokeMaterial = null;
            _emberMaterial = null;
            _heatMaterial = null;
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
                    view.MakeParticles();
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
                    view._embers.Play();
                    view._heat.Play();
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
            _flameMaterial = FlameParticleStyle.CreateMaterial(FlameParticleStyle.Kind.Tongue, 2.4f, 0.25f);
            if (_flameMaterial == null) return false;
            _smokeMaterial = FlameParticleStyle.CreateMaterial(FlameParticleStyle.Kind.Smoke, 1f, 1f);
            _emberMaterial = FlameParticleStyle.CreateMaterial(FlameParticleStyle.Kind.Ember, 4f, 0f);
            _heatMaterial = FlameParticleStyle.CreateMaterial(FlameParticleStyle.Kind.Heat, 1f, 1f);
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

        private void MakeParticles()
        {
            // Tongues stay white in the vertex colour: the shader's heat ramp does the colouring and cooling.
            _flame = FlameParticleStyle.Create(transform, "SurfaceFlame", _flameMaterial, 128, FlameParticleStyle.Kind.Tongue,
                FlameParticleStyle.Fade(Color.white, new Color(1f, 0.8f, 0.7f), 1f, 0.1f),
                AnimationCurve.EaseInOut(0f, 0.7f, 1f, 0.35f), 0.35f);
            _smoke = FlameParticleStyle.Create(transform, "SurfaceSmoke", _smokeMaterial, 32, FlameParticleStyle.Kind.Smoke,
                FlameParticleStyle.Fade(new Color(0.09f, 0.08f, 0.07f), new Color(0.05f, 0.05f, 0.05f), 0.35f, 0.25f),
                AnimationCurve.Linear(0f, 0.5f, 1f, 1.8f), 0.4f);
            _embers = FlameParticleStyle.Create(transform, "SurfaceEmbers", _emberMaterial, 24, FlameParticleStyle.Kind.Ember,
                FlameParticleStyle.Fade(Color.white, Color.white, 1f, 0.05f),
                AnimationCurve.Linear(0f, 1f, 1f, 0.4f), 1.2f);
            _heat = FlameParticleStyle.Create(transform, "SurfaceHeat", _heatMaterial, 16, FlameParticleStyle.Kind.Heat,
                FlameParticleStyle.Fade(Color.white, Color.white, 1f, 0.3f),
                AnimationCurve.Linear(0f, 0.7f, 1f, 1.3f), 0.2f);
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
                if (_embers.isPlaying) _embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (_heat.isPlaying) _heat.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
            float dt = Mathf.Min(deltaTime, 0.05f);
            _light.intensity = (0.75f + Mathf.PerlinNoise(now * 9f, _id * 0.37f) * 0.35f) * fade;
            _credit += dt * 105f * fade;
            int count = Mathf.FloorToInt(_credit);
            _credit -= count;
            for (int i = 0; i < count; i++)
            {
                Vector2 disk = Random.insideUnitCircle * (NapalmPatchSet.Radius * 0.8f);
                // Tall tongues near the middle, low licks at the rim, like a real pool fire.
                float centre = 1f - disk.magnitude / NapalmPatchSet.Radius;
                Vector2 extent = _isBody
                    ? new Vector2(Random.Range(0.26f, 0.42f), Random.Range(0.4f, 0.65f))
                    : new Vector2(Random.Range(0.36f, 0.58f), Random.Range(0.5f, 0.85f) * (0.6f + centre * 0.8f));
                Vector3 baseline = _isBody
                    ? transform.position + Random.insideUnitSphere * 0.18f
                    : transform.TransformPoint(new Vector3(disk.x, disk.y, 0f)) + _normal * 0.06f;
                // Billboards are centred on their position: lift by half the height so the base sits on the surface.
                ParticleSystem.EmitParams flame = new()
                {
                    position = baseline + Vector3.up * (extent.y * 0.4f),
                    velocity = Vector3.up * Random.Range(0.6f, 1.1f) + _normal * 0.1f,
                    startLifetime = Random.Range(0.45f, 0.8f),
                    startSize3D = new Vector3(extent.x, extent.y, 1f),
                    rotation = Random.Range(-12f, 12f),
                    startColor = Color.white,
                };
                _flame.Emit(flame, 1);
                if (Random.value < 0.12f)
                {
                    float size = Random.Range(0.45f, 0.7f);
                    _smoke.Emit(new ParticleSystem.EmitParams
                    {
                        position = baseline + Vector3.up * (0.5f + Random.value * 0.3f),
                        velocity = Vector3.up * Random.Range(0.4f, 0.7f),
                        startLifetime = Random.Range(1.2f, 1.8f),
                        startSize = size,
                        rotation = Random.Range(0f, 360f),
                        startColor = Color.white,
                    }, 1);
                }
            }
            _heatCredit += dt * 6f * fade;
            while (_heatCredit >= 1f)
            {
                _heatCredit -= 1f;
                Vector2 disk = Random.insideUnitCircle * (NapalmPatchSet.Radius * 0.5f);
                Vector3 origin = _isBody
                    ? transform.position + Vector3.up * 0.4f
                    : transform.TransformPoint(new Vector3(disk.x, disk.y, 0f)) + _normal * 0.1f + Vector3.up * 0.7f;
                _heat.Emit(new ParticleSystem.EmitParams
                {
                    position = origin, velocity = Vector3.up * Random.Range(0.5f, 0.8f),
                    startLifetime = Random.Range(1f, 1.4f), startSize = Random.Range(1f, 1.4f), startColor = Color.white,
                }, 1);
            }
            _emberCredit += dt * 7f * fade;
            while (_emberCredit >= 1f)
            {
                _emberCredit -= 1f;
                Vector2 disk = Random.insideUnitCircle * (NapalmPatchSet.Radius * 0.7f);
                Vector3 origin = _isBody
                    ? transform.position + Random.insideUnitSphere * 0.2f
                    : transform.TransformPoint(new Vector3(disk.x, disk.y, 0f)) + Vector3.up * 0.2f;
                _embers.Emit(new ParticleSystem.EmitParams
                {
                    position = origin,
                    velocity = Vector3.up * Random.Range(1f, 2f) + Random.insideUnitSphere * 0.4f,
                    startLifetime = Random.Range(0.8f, 1.5f),
                    startSize = Random.Range(0.025f, 0.05f),
                    startColor = Color.white,
                }, 1);
            }
        }
    }
}
