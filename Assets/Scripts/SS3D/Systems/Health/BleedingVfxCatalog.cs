using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Shared blood VFX assets (splatter textures, particle material, decal material) loaded from Resources.
    /// </summary>
    public sealed class BleedingVfxCatalog : ScriptableObject
    {
        // Match WoundVfx particle blood; white mask splatters are multiplied by this.
        private static readonly Color DecalTint = new(90f / 255f, 8f / 255f, 10f / 255f, 1f);

        [SerializeField] private Texture2D[] _splatters;
        [SerializeField] private Material _particleMaterial;
        [SerializeField] private Material _bloodDecalMaterial;
        [SerializeField] private GameObject _floorDecalPrefab;
        [SerializeField] private Shader _multiplyTintShader;

        private static BleedingVfxCatalog _instance;
        private readonly Dictionary<int, Texture2D> _tintedSplatters = new();
        private Material _tintBlitMaterial;

        public static BleedingVfxCatalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<BleedingVfxCatalog>(nameof(BleedingVfxCatalog));
                }

                return _instance;
            }
        }

        public Material ParticleMaterial => _particleMaterial;

        public Material BloodDecalMaterial => _bloodDecalMaterial;

        public GameObject FloorDecalPrefab => _floorDecalPrefab;

        public Texture2D PickRandomSplatter()
        {
            if (_splatters == null || _splatters.Length == 0)
            {
                return null;
            }

            return _splatters[Random.Range(0, _splatters.Length)];
        }

        public Material CreateDecalMaterial()
        {
            if (_bloodDecalMaterial == null)
            {
                return null;
            }

            Texture2D splatter = PickRandomSplatter();
            Material instance = new Material(_bloodDecalMaterial);
            if (splatter != null)
            {
                Texture2D tinted = GetTintedSplatter(splatter);
                if (instance.HasProperty("Base_Map"))
                {
                    instance.SetTexture("Base_Map", tinted);
                }

                if (instance.HasProperty("_BaseMap"))
                {
                    instance.SetTexture("_BaseMap", tinted);
                }

                if (instance.HasProperty("_MainTex"))
                {
                    instance.SetTexture("_MainTex", tinted);
                }
            }

            return instance;
        }

        private Texture2D GetTintedSplatter(Texture2D source)
        {
            if (source == null)
            {
                return null;
            }

            // White mask splatters need a runtime multiply because URP Decal has no Base Color input.
            int id = source.GetInstanceID();
            if (_tintedSplatters.TryGetValue(id, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D tinted = TintSplatter(source);
            _tintedSplatters[id] = tinted;
            return tinted;
        }

        private Texture2D TintSplatter(Texture2D source)
        {
            Shader tintShader = _multiplyTintShader != null
                ? _multiplyTintShader
                : Shader.Find("Hidden/SS3D/MultiplyTint");
            if (tintShader == null)
            {
                return source;
            }

            if (_tintBlitMaterial == null)
            {
                _tintBlitMaterial = new Material(tintShader);
            }

            _tintBlitMaterial.SetColor("_Color", DecalTint);

            RenderTexture rt = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, rt, _tintBlitMaterial);

            Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, false)
            {
                name = source.name + "_BloodTint",
                wrapMode = source.wrapMode,
                filterMode = source.filterMode,
                anisoLevel = source.anisoLevel,
            };

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            result.Apply(true, true);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }
    }
}
