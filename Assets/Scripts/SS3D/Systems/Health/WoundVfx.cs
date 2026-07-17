using Coimbra;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Rendering.URP;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Per-zone bleeding VFX: particle streams, body wound decals, and floor blood accumulation.
    /// Intensity and floor drip cadence scale with synced <see cref="HealthSnapshot"/> bleed rates.
    /// </summary>
    public class WoundVfx : MonoBehaviour
    {
        // BleedingRateForSeverity(Severed) — used to normalize VFX intensity 0..1.
        private const float ReferenceBleedRate = 2f;
        private const float FloorDecalIntervalMinSeconds = 0.22f;
        private const float FloorDecalIntervalMaxSeconds = 1.2f;
        private const float BodyDecalSizeMin = 0.1f;
        private const float BodyDecalSizeMax = 0.22f;
        private const float ImpactBurstCountMin = 12f;
        private const float ImpactBurstCountMax = 40f;

        private static readonly Color BloodColor = new(200f / 255f, 18f / 255f, 28f / 255f, 1f);

        private readonly Dictionary<BodyZone, GameObject> _activeParticles = new();
        private readonly Dictionary<BodyZone, DecalProjector> _bodyDecals = new();
        private readonly Dictionary<BodyZone, Transform> _anchors = new();
        private readonly HashSet<BodyZone> _impactBurstPlayed = new();
        private readonly HashSet<BodyZone> _particlesInitialized = new();

        private GameObject _particlePrefab;
        private bool _anchorsBuilt;
        private HealthSnapshot _snapshot = HealthSnapshot.Default;
        private float _floorDecalTimer;

        public void ApplySnapshot(HealthSnapshot snapshot)
        {
            _snapshot = snapshot;
            EnsureAnchors();

            // Stop bleed VFX on death — particles/decals parented to bones while Kill() ragdolls
            // and disposes controllers have caused hard editor crashes.
            if (snapshot.State == HealthState.Dead)
            {
                ClearAllEffects();
                enabled = false;
                return;
            }

            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                BodyZone zone = (BodyZone)i;
                float bleedRate = snapshot.GetZoneBleedingRate(zone);
                SetZoneBleeding(zone, bleedRate);
            }

            if (!snapshot.IsBleeding)
            {
                _floorDecalTimer = 0f;
                _impactBurstPlayed.Clear();
                _particlesInitialized.Clear();
            }
        }

        private void ClearAllEffects()
        {
            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                DisableZoneEffects((BodyZone)i);
            }

            _floorDecalTimer = 0f;
            _impactBurstPlayed.Clear();
            _particlesInitialized.Clear();
        }

        private void Update()
        {
            if (!_snapshot.IsBleeding || !BloodDecalSpawner.IsSupported)
            {
                return;
            }

            _floorDecalTimer -= Time.deltaTime;
            if (_floorDecalTimer > 0f)
            {
                return;
            }

            _floorDecalTimer = FloorDecalIntervalSeconds(_snapshot.TotalBleedingRate);
            TrySpawnFloorDecal();
        }

        private static float FloorDecalIntervalSeconds(float totalBleedRate)
        {
            // Higher total bleed → shorter gap between floor stamps.
            float t = Mathf.Clamp01(totalBleedRate / ReferenceBleedRate);
            return Mathf.Lerp(FloorDecalIntervalMaxSeconds, FloorDecalIntervalMinSeconds, t);
        }

        private static float NormalizeBleedRate(float bleedRate)
        {
            return Mathf.Clamp01(bleedRate / ReferenceBleedRate);
        }

        private void TrySpawnFloorDecal()
        {
            if (!TryPickWeightedBleedingZone(out BodyZone zone, out float bleedRate))
            {
                return;
            }

            if (!_anchors.TryGetValue(zone, out Transform anchor) || anchor == null)
            {
                return;
            }

            float intensity = Mathf.Lerp(0.7f, 1.45f, NormalizeBleedRate(bleedRate));
            BloodDecalSpawner.SpawnAtAnchor(anchor, intensity);
        }

        private bool TryPickWeightedBleedingZone(out BodyZone zone, out float bleedRate)
        {
            zone = BodyZone.Chest;
            bleedRate = 0f;
            float total = _snapshot.TotalBleedingRate;
            if (total <= 0f)
            {
                return false;
            }

            float pick = Random.Range(0f, total);
            float running = 0f;
            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                BodyZone candidate = (BodyZone)i;
                float rate = _snapshot.GetZoneBleedingRate(candidate);
                if (rate <= 0f)
                {
                    continue;
                }

                running += rate;
                if (pick <= running)
                {
                    zone = candidate;
                    bleedRate = rate;
                    return true;
                }
            }

            return false;
        }

        private void EnsureAnchors()
        {
            if (_anchorsBuilt)
            {
                return;
            }

            // Prefer ZoneTargetCollider transforms — they live on armature bones and follow
            // the skinned pose. AnatomyNode roots are body-part prefab pivots that stay at
            // bind-pose offsets beside the visible mesh.
            ZoneTargetCollider[] zoneColliders = GetComponentsInChildren<ZoneTargetCollider>(true);
            for (int i = 0; i < zoneColliders.Length; i++)
            {
                ZoneTargetCollider zoneCollider = zoneColliders[i];
                PreferDistalAnchor(zoneCollider.Zone, zoneCollider.transform);
            }

            AnatomyNode[] anatomyNodes = GetComponentsInChildren<AnatomyNode>(true);
            for (int i = 0; i < anatomyNodes.Length; i++)
            {
                AnatomyNode node = anatomyNodes[i];
                _anchors.TryAdd(node.PrimaryZone, node.transform);
            }

            _anchorsBuilt = true;
        }

        /// <summary>
        /// Keep the deepest (most distal) collider per zone so bleed VFX sit on the limb,
        /// not a proximal BodyCollider proxy when both exist.
        /// </summary>
        private void PreferDistalAnchor(BodyZone zone, Transform candidate)
        {
            if (!_anchors.TryGetValue(zone, out Transform existing) || existing == null)
            {
                _anchors[zone] = candidate;
                return;
            }

            if (GetHierarchyDepth(candidate) > GetHierarchyDepth(existing))
            {
                _anchors[zone] = candidate;
            }
        }

        private static int GetHierarchyDepth(Transform transform)
        {
            int depth = 0;
            Transform current = transform;
            while (current.parent != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private void SetZoneBleeding(BodyZone zone, float bleedRate)
        {
            if (bleedRate <= 0f)
            {
                DisableZoneEffects(zone);
                _impactBurstPlayed.Remove(zone);
                return;
            }

            if (!_anchors.TryGetValue(zone, out Transform anchor) || anchor == null)
            {
                return;
            }

            bool playImpactBurst = _impactBurstPlayed.Add(zone);
            EnableParticle(zone, anchor, bleedRate, playImpactBurst);
            EnableBodyDecal(zone, anchor, bleedRate);
        }

        private void DisableZoneEffects(BodyZone zone)
        {
            if (_activeParticles.TryGetValue(zone, out GameObject existing))
            {
                existing.SetActive(false);
            }

            _particlesInitialized.Remove(zone);

            if (_bodyDecals.TryGetValue(zone, out DecalProjector bodyDecal) && bodyDecal != null)
            {
                bodyDecal.gameObject.SetActive(false);
            }
        }

        private void EnableParticle(BodyZone zone, Transform anchor, float bleedRate, bool playImpactBurst)
        {
            bool isNew = !_activeParticles.TryGetValue(zone, out GameObject particle) || particle == null;
            bool wasInactive = !isNew && !particle.activeSelf;

            if (isNew)
            {
                GameObject prefab = GetParticlePrefab();
                if (prefab == null)
                {
                    return;
                }

                particle = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
                _activeParticles[zone] = particle;
            }

            particle.transform.SetParent(anchor, false);
            particle.transform.localPosition = Vector3.zero;
            particle.transform.localRotation = Quaternion.identity;

            ParticleSystem particleSystem = particle.GetComponentInChildren<ParticleSystem>();
            bool needsFullSetup = isNew || wasInactive || !_particlesInitialized.Contains(zone);
            if (needsFullSetup)
            {
                if (particleSystem != null && particleSystem.isPlaying)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                InitializeParticle(particleSystem, bleedRate, playImpactBurst);
                _particlesInitialized.Add(zone);
            }
            else
            {
                UpdateParticleIntensity(particleSystem, bleedRate);
            }

            particle.SetActive(true);

            if (particleSystem != null && !particleSystem.isPlaying)
            {
                particleSystem.Play();
            }
        }

        private void EnableBodyDecal(BodyZone zone, Transform anchor, float bleedRate)
        {
            if (!BloodDecalSpawner.IsSupported)
            {
                return;
            }

            if (!_bodyDecals.TryGetValue(zone, out DecalProjector decal) || decal == null)
            {
                var decalObject = new GameObject($"BloodWoundDecal_{zone}");
                decalObject.transform.SetParent(anchor, false);
                decalObject.transform.localPosition = Vector3.zero;
                decalObject.transform.localRotation = Quaternion.identity;

                decal = decalObject.AddComponent<DecalProjector>();
                decal.scaleMode = DecalScaleMode.ScaleInvariant;
                decal.drawDistance = 24f;
                decal.startAngleFade = 180f;
                decal.endAngleFade = 180f;
                decal.renderingLayerMask = DecalRenderingLayers.CharacterProjectorMask;
                decal.material = BloodDecalSpawner.CreateBodyDecalMaterial();
                _bodyDecals[zone] = decal;
            }
            else if (!decal.gameObject.activeSelf)
            {
                Material previous = decal.material;
                Material template = BloodDecalSpawner.BloodDecalMaterial;
                decal.material = BloodDecalSpawner.CreateBodyDecalMaterial();
                if (previous != null && previous != template)
                {
                    Destroy(previous);
                }
            }

            float t = NormalizeBleedRate(bleedRate);
            float size = Mathf.Lerp(BodyDecalSizeMin, BodyDecalSizeMax, t);
            decal.size = new Vector3(size, size, 0.35f);
            decal.fadeFactor = Mathf.Lerp(0.75f, 1f, t);
            decal.gameObject.SetActive(true);
        }

        private static void InitializeParticle(ParticleSystem particleSystem, float bleedRate, bool playImpactBurst)
        {
            if (particleSystem == null)
            {
                return;
            }

            float t = NormalizeBleedRate(bleedRate);

            ParticleSystem.MainModule main = particleSystem.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.7f, 1.1f, t),
                Mathf.Lerp(1.2f, 1.8f, t));
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.2f, 0.55f, t),
                Mathf.Lerp(0.55f, 1.35f, t));
            main.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.045f, 0.07f, t),
                Mathf.Lerp(0.08f, 0.14f, t));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = BloodColor;
            main.gravityModifier = Mathf.Lerp(2.2f, 3.2f, t);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.RoundToInt(Mathf.Lerp(48f, 120f, t));
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            UpdateParticleIntensity(particleSystem, bleedRate);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(BloodColor, 0f), new GradientColorKey(BloodColor, 1f) },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.45f),
                    new GradientAlphaKey(0.35f, 0.75f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.75f));

            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            Material particleMaterial = BleedingVfxCatalog.Instance != null
                ? BleedingVfxCatalog.Instance.ParticleMaterial
                : null;
            if (particleMaterial != null)
            {
                renderer.material = particleMaterial;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (playImpactBurst)
            {
                int burst = Mathf.RoundToInt(Mathf.Lerp(ImpactBurstCountMin, ImpactBurstCountMax, t));
                particleSystem.Emit(burst);
            }
        }

        private static void UpdateParticleIntensity(ParticleSystem particleSystem, float bleedRate)
        {
            if (particleSystem == null)
            {
                return;
            }

            float t = NormalizeBleedRate(bleedRate);

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = Mathf.Lerp(10f, 42f, t);

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Lerp(0.025f, 0.055f, t);
            shape.radiusThickness = 1f;
            shape.rotation = Vector3.zero;
            shape.scale = Vector3.one;

            ParticleSystem.MainModule main = particleSystem.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.2f, 0.55f, t),
                Mathf.Lerp(0.55f, 1.35f, t));
            main.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.045f, 0.07f, t),
                Mathf.Lerp(0.08f, 0.14f, t));
            main.maxParticles = Mathf.RoundToInt(Mathf.Lerp(48f, 120f, t));
        }

        private GameObject GetParticlePrefab()
        {
            if (_particlePrefab == null)
            {
                _particlePrefab = Assets.Get<GameObject>(AssetDatabases.ParticlesEffects, ParticlesEffects.BleedingParticle);
            }

            return _particlePrefab;
        }

        private void OnDestroy()
        {
            foreach (GameObject particle in _activeParticles.Values)
            {
                if (particle != null)
                {
                    particle.Dispose(true);
                }
            }

            _activeParticles.Clear();

            Material template = BloodDecalSpawner.BloodDecalMaterial;
            foreach (DecalProjector decal in _bodyDecals.Values)
            {
                if (decal == null)
                {
                    continue;
                }

                if (decal.material != null && decal.material != template)
                {
                    Destroy(decal.material);
                }

                decal.gameObject.Dispose(true);
            }

            _bodyDecals.Clear();
        }
    }
}
