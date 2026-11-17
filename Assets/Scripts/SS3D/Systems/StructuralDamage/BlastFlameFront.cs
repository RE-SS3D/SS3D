using Coimbra;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// A brief sheet of flame over every tile a blast reached (<see cref="BlastFlameTiles"/>), catching outward from the
    /// epicenter one tile per <see cref="BlastFlameTiles.HopSeconds"/>. It follows the blast's real shape, so fire
    /// runs down a corridor and stops at a wall that held. Client presentation only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlastFlameFront : MonoBehaviour
    {
        /// <summary>Seconds a tile's flames last.</summary>
        public const float MinLifetime = 0.35f;
        public const float MaxLifetime = 0.7f;

        private const int ParticlesPerTile = 3;

        private static Material _material;
        private ParticleSystem _particles;

        public static void Spawn(Vector3 epicenter, byte[] packedTiles)
        {
            List<BlastFlameTiles.Flame> flames = BlastFlameTiles.Unpack(packedTiles);
            if (flames.Count == 0 || !EnsureMaterial())
                return;

            GameObject root = new("BlastFlameFront");
            root.transform.position = epicenter;
            root.AddComponent<BlastFlameFront>().Play(epicenter, flames);
        }

        private void Play(Vector3 epicenter, List<BlastFlameTiles.Flame> flames)
        {
            flames.Sort((a, b) => a.Delay.CompareTo(b.Delay));
            _particles = gameObject.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = flames.Count * ParticlesPerTile;
            main.gravityModifier = -0.12f;

            var emission = _particles.emission;
            emission.enabled = false;
            var shape = _particles.shape;
            shape.enabled = false;

            var colour = _particles.colorOverLifetime;
            colour.enabled = true;
            Color core = BlastVfxCatalog.FireCoreColor;
            Gradient gradient = new();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f),
                    new GradientColorKey(core, 0.3f),
                    new GradientColorKey(new Color(core.r * 0.5f, core.g * 0.2f, core.b * 0.08f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.9f, 0.08f),
                    new GradientAlphaKey(0.7f, 0.5f),
                    new GradientAlphaKey(0f, 1f),
                });
            colour.color = gradient;

            var size = _particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.25f, 1f), new Keyframe(1f, 1.25f)));

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            _particles.Play();
            StartCoroutine(Ignite(epicenter, flames));
        }

        private IEnumerator Ignite(Vector3 epicenter, List<BlastFlameTiles.Flame> flames)
        {
            float start = Time.time;
            int next = 0;
            while (next < flames.Count)
            {
                float elapsed = Time.time - start;
                while (next < flames.Count && flames[next].Delay <= elapsed)
                    EmitTile(epicenter, flames[next++]);
                yield return null;
            }

            yield return new WaitForSeconds(MaxLifetime + 0.1f);
            gameObject.Dispose(true);
        }

        private void EmitTile(Vector3 epicenter, BlastFlameTiles.Flame flame)
        {
            // Weak edge tiles get smaller, shorter flames, so the fire thins out where the blast ran out.
            float strength = Mathf.Sqrt(flame.Strength);
            Vector3 centre = epicenter + new Vector3(flame.Offset.x, 0f, flame.Offset.y);
            for (int i = 0; i < ParticlesPerTile; i++)
            {
                var emit = new ParticleSystem.EmitParams
                {
                    position = centre + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(0.25f, 0.9f),
                        Random.Range(-0.35f, 0.35f)),
                    velocity = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.6f, 1.6f), Random.Range(-0.3f, 0.3f)),
                    startSize = Mathf.Lerp(0.55f, 1.35f, strength) * Random.Range(0.8f, 1.15f),
                    startLifetime = Mathf.Lerp(MinLifetime, MaxLifetime, strength) * Random.Range(0.85f, 1.1f),
                    // Additive and overlapping: three per tile on hundreds of tiles would wash out to white.
                    startColor = new Color(1f, 1f, 1f, 0.45f),
                    rotation = Random.Range(0f, 360f),
                };
                _particles.Emit(emit, 1);
            }
        }

        private static bool EnsureMaterial()
        {
            if (_material != null)
                return true;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return false;

            Shader fire = Resources.Load<Shader>("StructuralDamage/BlastFire");
            if (fire == null || !fire.isSupported)
                return false;

            _material = new Material(fire) { name = "Blast flame front", hideFlags = HideFlags.HideAndDontSave };
            return true;
        }
    }
}
