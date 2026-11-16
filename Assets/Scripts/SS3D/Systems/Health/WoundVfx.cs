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
    /// Per-zone bleeding VFX: continuous trickle streams, on-hit impact spray,
    /// body wound decals, and floor blood accumulation.
    /// Trickle intensity scales with synced <see cref="HealthSnapshot"/> bleed rates;
    /// impact spray is driven by damage Rpc (not bleed-onset alone).
    /// </summary>
    public class WoundVfx : MonoBehaviour
    {
        // BleedingRateForSeverity(Severed) — used to normalize VFX intensity 0..1.
        private const float ReferenceBleedRate = 2f;
        private const float FloorDecalIntervalMinSeconds = 0.22f;
        private const float FloorDecalIntervalMaxSeconds = 1.2f;
        private const float BodyDecalSizeMin = 0.14f;
        private const float BodyDecalSizeMax = 0.28f;
        private const float BodyDecalOutwardOffset = 0.07f;
        private const float BodyDecalProjectionDepth = 0.55f;
        private const float ImpactBurstCountMin = 12f;
        private const float ImpactBurstCountMax = 36f;
        private const float ImpactSpeedMin = 2.4f;
        private const float ImpactSpeedMax = 6.5f;
        private const float ImpactLifetimeMin = 0.15f;
        private const float ImpactLifetimeMax = 0.4f;
        private const float ImpactSizeMin = 0.015f;
        private const float ImpactSizeMax = 0.04f;
        // Color-over-lifetime is normalized 0..1; ~0.05 ≈ 40–80 ms for drip lifetimes,
        // so droplets clear the mesh before becoming visible.
        private const float DripSpawnInvisibleLifetimeFraction = 0.05f;

        private readonly Dictionary<BodyZone, GameObject> _activeParticles = new();
        private readonly Dictionary<BodyZone, DecalProjector> _bodyDecals = new();
        private readonly Dictionary<BodyZone, float> _bodyDecalSpin = new();
        private readonly Dictionary<BodyZone, Transform> _anchors = new();
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
            }
        }

        /// <summary>
        /// Immediate directional blood spray for a hit. Safe to call on every meaningful
        /// brute hit — independent of whether the zone has started continuous bleeding.
        /// </summary>
        public void PlayImpactBurst(BodyZone zone, float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            if (intensity <= 0f)
            {
                return;
            }

            EnsureAnchors();
            if (!_anchors.TryGetValue(zone, out Transform anchor) || anchor == null)
            {
                return;
            }

            ParticleSystem particleSystem = GetOrCreateParticleSystem(zone, anchor);
            if (particleSystem == null)
            {
                return;
            }

            if (!_particlesInitialized.Contains(zone))
            {
                InitializeParticle(particleSystem, _snapshot.GetZoneBleedingRate(zone));
                _particlesInitialized.Add(zone);
            }

            EmitImpactSpray(particleSystem, anchor, intensity);

            if (!particleSystem.isPlaying)
            {
                particleSystem.Play();
            }
        }

        private void ClearAllEffects()
        {
            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                DisableZoneEffects((BodyZone)i);
            }

            _floorDecalTimer = 0f;
            _particlesInitialized.Clear();
        }

        private void Update()
        {
            if (!_snapshot.IsBleeding || !BloodDecalSpawner.IsSupported)
            {
                return;
            }

            // Keep wound projectors aimed into the mesh as bones animate — identity
            // local rotation on the bone rarely intersects skin/clothing.
            RefreshActiveBodyDecals();

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

        private Vector3 ComputeOutward(Transform anchor)
        {
            Vector3 outward = anchor.position - transform.position;
            outward.y *= 0.35f;
            if (outward.sqrMagnitude < 0.0001f)
            {
                outward = transform.forward;
            }

            return outward.normalized;
        }

        private void SetZoneBleeding(BodyZone zone, float bleedRate)
        {
            if (bleedRate <= 0f)
            {
                DisableZoneEffects(zone);
                return;
            }

            if (!_anchors.TryGetValue(zone, out Transform anchor) || anchor == null)
            {
                return;
            }

            EnableParticle(zone, anchor, bleedRate);
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

            _bodyDecalSpin.Remove(zone);
        }

        private void RefreshActiveBodyDecals()
        {
            foreach (KeyValuePair<BodyZone, DecalProjector> pair in _bodyDecals)
            {
                DecalProjector decal = pair.Value;
                if (decal == null || !decal.gameObject.activeSelf)
                {
                    continue;
                }

                if (!_anchors.TryGetValue(pair.Key, out Transform anchor) || anchor == null)
                {
                    continue;
                }

                OrientBodyDecal(decal, anchor, pair.Key);
            }
        }

        private void OrientBodyDecal(DecalProjector decal, Transform anchor, BodyZone zone)
        {
            Vector3 outward = ComputeOutward(anchor);
            if (!_bodyDecalSpin.TryGetValue(zone, out float spin))
            {
                spin = Random.Range(0f, 360f);
                _bodyDecalSpin[zone] = spin;
            }

            // Same into-surface convention as floor stamps: project along -normal.
            Quaternion rotation = BloodDecalSpawner.RotationOntoSurface(outward, spin);
            decal.transform.SetPositionAndRotation(
                anchor.position + outward * BodyDecalOutwardOffset,
                rotation);
        }

        private void EnableParticle(BodyZone zone, Transform anchor, float bleedRate)
        {
            ParticleSystem particleSystem = GetOrCreateParticleSystem(zone, anchor);
            if (particleSystem == null)
            {
                return;
            }

            GameObject particle = _activeParticles[zone];
            bool needsFullSetup = !_particlesInitialized.Contains(zone);
            if (needsFullSetup)
            {
                InitializeParticle(particleSystem, bleedRate);
                _particlesInitialized.Add(zone);
            }
            else
            {
                UpdateParticleIntensity(particleSystem, anchor, bleedRate);
            }

            particle.SetActive(true);

            if (!particleSystem.isPlaying)
            {
                particleSystem.Play();
            }
        }

        private ParticleSystem GetOrCreateParticleSystem(BodyZone zone, Transform anchor)
        {
            bool isNew = !_activeParticles.TryGetValue(zone, out GameObject particle) || particle == null;
            if (isNew)
            {
                GameObject prefab = GetParticlePrefab();
                if (prefab == null)
                {
                    return null;
                }

                particle = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
                _activeParticles[zone] = particle;

                // Prefab may playOnAwake — stop before any duration/loop writes.
                ParticleSystem spawned = particle.GetComponentInChildren<ParticleSystem>();
                if (spawned != null)
                {
                    ParticleSystem.MainModule spawnedMain = spawned.main;
                    spawnedMain.playOnAwake = false;
                    spawned.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            particle.transform.SetParent(anchor, false);
            particle.transform.localPosition = Vector3.zero;
            particle.transform.localRotation = Quaternion.identity;
            particle.SetActive(true);

            return particle.GetComponentInChildren<ParticleSystem>();
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
                decalObject.transform.SetParent(anchor, true);

                decal = decalObject.AddComponent<DecalProjector>();
                decal.scaleMode = DecalScaleMode.ScaleInvariant;
                decal.drawDistance = 24f;
                decal.startAngleFade = 180f;
                decal.endAngleFade = 180f;
                decal.pivot = new Vector3(0f, 0f, BodyDecalProjectionDepth * 0.35f);
                // Mask first, then material — same DecalEntityManager refresh order as floor.
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
            decal.size = new Vector3(size, size, BodyDecalProjectionDepth);
            decal.fadeFactor = Mathf.Lerp(0.75f, 1f, t);
            OrientBodyDecal(decal, anchor, zone);
            decal.gameObject.SetActive(true);
        }

        private void InitializeParticle(ParticleSystem particleSystem, float bleedRate)
        {
            if (particleSystem == null)
            {
                return;
            }

            // Duration/loop cannot change while playing — impact spray hits this path
            // when the prefab auto-starts or a prior emit left the system running.
            if (particleSystem.isPlaying || particleSystem.particleCount > 0)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            float t = NormalizeBleedRate(bleedRate);

            ParticleSystem.MainModule main = particleSystem.main;
            main.playOnAwake = false;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.7f, 1.1f, t),
                Mathf.Lerp(1.2f, 1.8f, t));
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.25f, 0.5f, t),
                Mathf.Lerp(0.55f, 1.1f, t));
            main.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.016f, 0.026f, t),
                Mathf.Lerp(0.03f, 0.045f, t));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = BleedingVfxCatalog.BloodColor;
            main.gravityModifier = Mathf.Lerp(2.2f, 3.2f, t);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.RoundToInt(Mathf.Lerp(32f, 72f, t));
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(BleedingVfxCatalog.BloodColor, 0f),
                    new GradientColorKey(BleedingVfxCatalog.BloodColor, 1f),
                },
                new[]
                {
                    // Start invisible so continuous drips aren't seen popping on the skinned mesh.
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, DripSpawnInvisibleLifetimeFraction),
                    new GradientAlphaKey(1f, 0.45f),
                    new GradientAlphaKey(0.35f, 0.75f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradient;

            // Soft droplet shrink — texture is a round falloff, not a splat mask.
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                AnimationCurve.EaseInOut(0f, 1f, 1f, 0.55f));

            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            Material particleMaterial = BleedingVfxCatalog.Instance != null
                ? BleedingVfxCatalog.Instance.ParticleMaterial
                : null;
            if (particleMaterial != null)
            {
                renderer.material = particleMaterial;
            }

            // Mild stretch — enough for droplet motion, not huge streaks.
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.06f;
            renderer.lengthScale = 1.05f;
            renderer.cameraVelocityScale = 0f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Transform anchor = particleSystem.transform.parent != null
                ? particleSystem.transform.parent
                : transform;
            UpdateParticleIntensity(particleSystem, anchor, bleedRate);
        }

        private void UpdateParticleIntensity(ParticleSystem particleSystem, Transform anchor, float bleedRate)
        {
            if (particleSystem == null)
            {
                return;
            }

            float t = NormalizeBleedRate(bleedRate);
            Vector3 outward = ComputeOutward(anchor);

            // Moderate drip volume — smaller droplets, wider cone.
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = bleedRate > 0f
                ? Mathf.Lerp(5f, 16f, t)
                : 0f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = Mathf.Lerp(28f, 42f, t);
            shape.radius = Mathf.Lerp(0.03f, 0.06f, t);
            shape.length = 0.1f;
            shape.radiusThickness = 1f;
            shape.arc = 360f;
            // Cone emits along +Z; aim slightly out from the body and down.
            Vector3 localEmit = particleSystem.transform.InverseTransformDirection(
                (outward + Vector3.down * 0.45f).normalized);
            if (localEmit.sqrMagnitude < 0.0001f)
            {
                localEmit = Vector3.down;
            }

            shape.rotation = Quaternion.LookRotation(localEmit).eulerAngles;
            shape.scale = Vector3.one;

            ParticleSystem.MainModule main = particleSystem.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.25f, 0.5f, t),
                Mathf.Lerp(0.55f, 1.1f, t));
            main.startSize = new ParticleSystem.MinMaxCurve(
                Mathf.Lerp(0.016f, 0.026f, t),
                Mathf.Lerp(0.03f, 0.045f, t));
            main.gravityModifier = Mathf.Lerp(2.2f, 3.2f, t);
            main.maxParticles = Mathf.RoundToInt(Mathf.Lerp(32f, 72f, t));

            // Soft lateral push so drips leave the mesh instead of swimming inside it.
            ParticleSystem.ForceOverLifetimeModule force = particleSystem.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.World;
            Vector3 push = outward * Mathf.Lerp(0.55f, 1.6f, t);
            force.x = push.x;
            force.y = push.y * 0.2f;
            force.z = push.z;
        }

        private void EmitImpactSpray(ParticleSystem particleSystem, Transform anchor, float intensity)
        {
            int count = Mathf.RoundToInt(Mathf.Lerp(ImpactBurstCountMin, ImpactBurstCountMax, intensity));
            Vector3 outward = ComputeOutward(anchor);
            Vector3 origin = anchor.position + outward * 0.04f;

            for (int i = 0; i < count; i++)
            {
                Vector3 jitter = Random.insideUnitSphere;
                Vector3 dir = (outward * 0.85f + Vector3.up * 0.25f + jitter * 1.05f).normalized;
                float speed = Mathf.Lerp(ImpactSpeedMin, ImpactSpeedMax, intensity)
                    * Random.Range(0.65f, 1.2f);

                var emit = new ParticleSystem.EmitParams
                {
                    position = origin + jitter * 0.03f,
                    velocity = dir * speed,
                    startLifetime = Mathf.Lerp(ImpactLifetimeMin, ImpactLifetimeMax, intensity)
                        * Random.Range(0.75f, 1.15f),
                    startSize = Random.Range(
                        ImpactSizeMin,
                        Mathf.Lerp(ImpactSizeMin * 1.4f, ImpactSizeMax, intensity)),
                    startColor = BleedingVfxCatalog.BloodColor,
                    applyShapeToPosition = false,
                };

                particleSystem.Emit(emit, 1);
            }
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
