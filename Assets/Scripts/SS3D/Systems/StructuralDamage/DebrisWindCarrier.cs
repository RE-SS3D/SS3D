using SS3D.Rendering.URP;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Carries one debris burst's fragments with the atmos wind under each of them, every frame, and lets them float
    /// once they cross into vacuum. Follows the field rather than sampling at spawn, because the debris a breach
    /// throws exists before the air through that breach starts moving.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class DebrisWindCarrier : MonoBehaviour
    {
        private ParticleSystem _particles;
        private ParticleSystem.Particle[] _buffer;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
        }

        private void LateUpdate()
        {
            if (AtmosRenderContext.IsRenderingSuppressed
                || !AtmosRenderContext.TryGetSnapshot(out AtmosRenderContext.Snapshot snapshot))
                return;

            int capacity = _particles.main.maxParticles;
            if (_buffer == null || _buffer.Length < capacity)
                _buffer = new ParticleSystem.Particle[capacity];

            int count = _particles.GetParticles(_buffer);
            if (count == 0)
                return;

            float dt = Time.deltaTime;
            Vector3 antiGravity = -Physics.gravity * (_particles.main.gravityModifierMultiplier * dt);
            for (int i = 0; i < count; i++)
            {
                ParticleSystem.Particle particle = _buffer[i];
                DebrisSpriteVfx.SampleAir(snapshot, particle.position, out Vector3 drift, out bool vacuum);
                // Each fragment catches the wind a little differently, stable for its life.
                float catchShare = 0.7f + (particle.randomSeed % 1000) * 0.0006f;
                particle.position += drift * (catchShare * dt);
                if (vacuum)
                    particle.velocity += antiGravity;
                _buffer[i] = particle;
            }

            _particles.SetParticles(_buffer, count);
        }
    }
}
