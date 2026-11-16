using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Drives <c>_Lumin</c> / <c>_EmissionColor</c> through per-material <see cref="MaterialPropertyBlock"/>s,
    /// keeping each material's own powered values. A single renderer-wide block seeded from the first
    /// emissive material wrote e.g. <c>Palette</c>'s black emission onto the <c>PaletteEmission</c> submesh,
    /// so multi-material machines never glowed. Plain class: create it from Awake/Start, not a field initializer
    /// (Unity disallows constructing a MaterialPropertyBlock during serialization).
    /// </summary>
    public sealed class EmissiveMaterialSlots
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int LuminId = Shader.PropertyToID("_Lumin");

        private struct Slot
        {
            public Renderer Renderer;
            public int MaterialIndex;
            public float Lumin;
            public Color Emission;
            public bool HasLumin;
            public bool HasEmission;
        }

        private readonly List<Slot> _slots = new();
        private MaterialPropertyBlock _block;
        private bool _hasApplied;
        private float _lastLuminScale;
        private Color _lastEmission;
        private bool _lastWasOverride;

        public int Count => _slots.Count;

        /// <summary>
        /// Records every material carrying <c>_Lumin</c> or <c>_EmissionColor</c> on <paramref name="renderers"/>.
        /// <paramref name="include"/> can narrow which materials count (e.g. only <c>_EMISSION</c>-keyword ones).
        /// </summary>
        public void Cache(IEnumerable<Renderer> renderers, Func<Material, bool> include = null)
        {
            _slots.Clear();
            _hasApplied = false;
            if (renderers == null)
                return;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null || (include != null && !include(material)))
                        continue;

                    bool hasLumin = material.HasProperty(LuminId);
                    bool hasEmission = material.HasProperty(EmissionColorId);
                    if (!hasLumin && !hasEmission)
                        continue;

                    _slots.Add(new Slot
                    {
                        Renderer = renderer,
                        MaterialIndex = i,
                        Lumin = hasLumin ? material.GetFloat(LuminId) : 0f,
                        Emission = hasEmission ? material.GetColor(EmissionColorId) : Color.black,
                        HasLumin = hasLumin,
                        HasEmission = hasEmission,
                    });
                }
            }
        }

        /// <summary>Each material's powered lumin × <paramref name="luminScale"/>, emission × <paramref name="emissionTint"/>.</summary>
        public void Apply(float luminScale, Color emissionTint) => Write(luminScale, emissionTint, isOverride: false);

        /// <summary>Each material's powered lumin × <paramref name="luminScale"/>; emission replaced by <paramref name="emission"/>.</summary>
        public void ApplyEmissionOverride(float luminScale, Color emission) => Write(luminScale, emission, isOverride: true);

        /// <summary>Shorthand for fully powered (material values) or fully dark.</summary>
        public void ApplyPowered(bool powered) => Apply(powered ? 1f : 0f, powered ? Color.white : Color.black);

        private void Write(float luminScale, Color emission, bool isOverride)
        {
            if (_hasApplied
                && _lastWasOverride == isOverride
                && Mathf.Approximately(_lastLuminScale, luminScale)
                && _lastEmission == emission)
            {
                return;
            }

            _hasApplied = true;
            _lastLuminScale = luminScale;
            _lastEmission = emission;
            _lastWasOverride = isOverride;
            _block ??= new MaterialPropertyBlock();

            foreach (Slot slot in _slots)
            {
                if (slot.Renderer == null)
                    continue;

                slot.Renderer.GetPropertyBlock(_block, slot.MaterialIndex);
                if (slot.HasLumin)
                    _block.SetFloat(LuminId, slot.Lumin * luminScale);

                if (slot.HasEmission)
                    _block.SetColor(EmissionColorId, isOverride ? emission : Multiply(slot.Emission, emission));

                slot.Renderer.SetPropertyBlock(_block, slot.MaterialIndex);
            }
        }

        private static Color Multiply(Color a, Color b) => new(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
    }
}
