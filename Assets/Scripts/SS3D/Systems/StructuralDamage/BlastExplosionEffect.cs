using Coimbra;
using System.Collections;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Short-lived fireball wash at a blast epicenter: particles and an Effect-tier point light.
    /// The boom plays from <see cref="StructuralAudioPresenter"/>, on the occluded pool.
    /// Configures particle systems in code so the prefab stays thin.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlastExplosionEffect : MonoBehaviour
    {
        private Light _light;
        private ParticleSystem _core;
        private ParticleSystem _smoke;
        private bool _configured;

        public void Play(float yieldScale, BlastVfxCatalog catalog)
        {
            EnsureConfigured(catalog);
            float scale = Mathf.Max(0.35f, yieldScale);

            if (_core != null)
            {
                ParticleSystem.MainModule main = _core.main;
                main.startSizeMultiplier = scale;
                main.startSpeedMultiplier = scale;
                _core.Play(true);
            }

            if (_smoke != null)
            {
                ParticleSystem.MainModule smokeMain = _smoke.main;
                smokeMain.startSizeMultiplier = scale;
                _smoke.Play(true);
            }

            if (_light != null && catalog != null)
            {
                _light.enabled = true;
                _light.color = BlastVfxCatalog.FireCoreColor;
                _light.intensity = catalog.LightIntensity * scale;
                _light.range = catalog.LightRange * Mathf.Lerp(0.85f, 1.25f, (scale - 0.35f) / 2.15f);
                StopAllCoroutines();
                StartCoroutine(FadeLight(catalog.LightDuration, catalog.LightIntensity * scale));
            }

            float lifetime = catalog != null ? catalog.EffectLifetime : 1.2f;
            StartCoroutine(DisposeAfter(lifetime * Mathf.Lerp(0.9f, 1.3f, (scale - 0.35f) / 2.15f)));
        }

        private IEnumerator DisposeAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.Dispose(true);
        }

        private void EnsureConfigured(BlastVfxCatalog catalog)
        {
            if (_configured)
                return;

            _configured = true;

            _light = GetComponent<Light>();
            if (_light == null)
            {
                _light = gameObject.AddComponent<Light>();
                _light.type = LightType.Point;
                _light.shadows = LightShadows.None;
            }

            _light.color = BlastVfxCatalog.FireCoreColor;
            _light.enabled = false;

            _core = FindOrCreateBurst("FireballCore", isSmoke: false, catalog);
            _smoke = FindOrCreateBurst("FireballSmoke", isSmoke: true, catalog);
        }

        private ParticleSystem FindOrCreateBurst(string childName, bool isSmoke, BlastVfxCatalog catalog)
        {
            Transform child = transform.Find(childName);
            GameObject host;
            if (child != null)
            {
                host = child.gameObject;
            }
            else
            {
                host = new GameObject(childName);
                host.transform.SetParent(transform, false);
                host.transform.localPosition = Vector3.zero;
            }

            ParticleSystem ps = host.GetComponent<ParticleSystem>();
            if (ps == null)
                ps = host.AddComponent<ParticleSystem>();

            ConfigureBurst(ps, isSmoke, catalog);
            return ps;
        }

        private static void ConfigureBurst(ParticleSystem ps, bool isSmoke, BlastVfxCatalog catalog)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = isSmoke ? 0.55f : 0.28f;
            main.startLifetime = isSmoke
                ? new ParticleSystem.MinMaxCurve(0.45f, 0.85f)
                : new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            main.startSpeed = isSmoke
                ? new ParticleSystem.MinMaxCurve(0.4f, 1.2f)
                : new ParticleSystem.MinMaxCurve(2.5f, 6f);
            main.startSize = isSmoke
                ? new ParticleSystem.MinMaxCurve(0.6f, 1.4f)
                : new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
            main.startColor = isSmoke
                ? new Color(0.25f, 0.22f, 0.2f, 0.55f)
                : Color.white;
            main.gravityModifier = isSmoke ? -0.15f : -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = isSmoke ? 24 : 48;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)(isSmoke ? 10 : 28)),
            });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = isSmoke ? 0.35f : 0.15f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            if (isSmoke)
            {
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.35f, 0.3f, 0.28f), 0f),
                        new GradientColorKey(new Color(0.15f, 0.14f, 0.13f), 1f),
                    },
                    new[]
                    {
                        new GradientAlphaKey(0.5f, 0f),
                        new GradientAlphaKey(0f, 1f),
                    });
            }
            else
            {
                Color core = BlastVfxCatalog.FireCoreColor;
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(core, 0.35f),
                        new GradientColorKey(new Color(core.r * 0.4f, core.g * 0.25f, core.b * 0.1f), 1f),
                    },
                    new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.85f, 0.25f),
                        new GradientAlphaKey(0f, 1f),
                    });
            }

            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, isSmoke ? 1.6f : 0.2f));

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(isSmoke ? 1.2f : 2.5f);

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (catalog != null && catalog.ParticleMaterial != null)
                renderer.sharedMaterial = catalog.ParticleMaterial;
        }

        private IEnumerator FadeLight(float duration, float startIntensity)
        {
            if (_light == null || duration <= 0f)
                yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Hold bright briefly, then fall off.
                float envelope = t < 0.15f ? 1f : 1f - (t - 0.15f) / 0.85f;
                _light.intensity = startIntensity * Mathf.Max(0f, envelope);
                yield return null;
            }

            _light.intensity = 0f;
            _light.enabled = false;
        }
    }
}
