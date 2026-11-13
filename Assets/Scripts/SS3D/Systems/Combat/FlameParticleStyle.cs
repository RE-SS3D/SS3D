using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Shared setup for <c>Combat/Flame</c> particles: camera-facing billboards with per-particle seed and age
    /// streams, so the shader animates every flame independently and cools it as it rises.
    /// </summary>
    public static class FlameParticleStyle
    {
        public enum Kind { Jet = 0, Tongue = 1, Smoke = 2, Ember = 3, Heat = 4 }

        private static readonly List<ParticleSystemVertexStream> Streams = new()
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,
            ParticleSystemVertexStream.StableRandomX,
            ParticleSystemVertexStream.AgePercent,
        };

        /// <summary>Null when the shader is missing (stripped server build).</summary>
        public static Material CreateMaterial(Kind kind, float intensity, float absorb)
        {
            Shader shader = Resources.Load<Shader>("Combat/Flame");
            if (shader == null) return null;
            Material material = new(shader) { name = $"Flame{kind}", hideFlags = HideFlags.HideAndDontSave };
            material.SetFloat("_Mode", (float)kind);
            material.SetFloat("_Intensity", intensity);
            material.SetFloat("_Absorb", absorb);
            material.SetFloat("_SoftDistance", kind == Kind.Smoke || kind == Kind.Heat ? 0.4f : 0.2f);
            // The opaque copy holds no transparents: haze must draw before the fire, never over it.
            if (kind == Kind.Heat) material.renderQueue = (int)RenderQueue.Transparent - 10;
            return material;
        }

        /// <summary>A world-space, script-emitted system with turbulence; callers emit with explicit params.</summary>
        public static ParticleSystem Create(Transform parent, string name, Material material, int capacity,
            Kind kind, Gradient color, AnimationCurve size, float turbulence)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            ParticleSystem system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = capacity;
            main.startSpeed = 0f;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;
            ParticleSystem.ColorOverLifetimeModule colorModule = system.colorOverLifetime;
            colorModule.enabled = true;
            colorModule.color = color;
            ParticleSystem.SizeOverLifetimeModule sizeModule = system.sizeOverLifetime;
            sizeModule.enabled = true;
            sizeModule.size = new ParticleSystem.MinMaxCurve(1f, size);
            if (turbulence > 0f)
            {
                ParticleSystem.NoiseModule noise = system.noise;
                noise.enabled = true;
                noise.strength = turbulence;
                noise.frequency = 1.6f;
                noise.scrollSpeed = 0.8f;
                noise.damping = true;
                noise.quality = ParticleSystemNoiseQuality.Medium;
            }
            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.SetActiveVertexStreams(Streams);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortMode = ParticleSystemSortMode.None;
            system.Play();
            return system;
        }

        public static Gradient Fade(Color start, Color end, float peakAlpha, float peakAt = 0.12f)
        {
            Gradient gradient = new();
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(peakAlpha, peakAt), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }
    }
}
