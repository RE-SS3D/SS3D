using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Shared bullet-hole decal assets loaded from Resources.
    /// </summary>
    public sealed class BulletHoleVfxCatalog : ScriptableObject
    {
        /// <summary>Dark charcoal for white-mask shred textures (URP Decal has no Base Color multiply).</summary>
        public static readonly Color HoleColor = new(28f / 255f, 26f / 255f, 24f / 255f, 1f);

        [SerializeField] private Texture2D[] _holes;
        [SerializeField] private Material _decalMaterial;
        [SerializeField] private GameObject _floorDecalPrefab;
        [SerializeField] private Shader _multiplyTintShader;

        private static BulletHoleVfxCatalog _instance;
        private readonly Dictionary<int, Texture2D> _tintedHoles = new();
        private Material _tintBlitMaterial;

        public static BulletHoleVfxCatalog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<BulletHoleVfxCatalog>(nameof(BulletHoleVfxCatalog));
                }

                return _instance;
            }
        }

        public Material DecalMaterial => _decalMaterial;

        public GameObject FloorDecalPrefab => _floorDecalPrefab;

        public Texture2D PickRandomHole()
        {
            if (_holes == null || _holes.Length == 0)
            {
                return null;
            }

            return _holes[Random.Range(0, _holes.Length)];
        }

        public Material CreateDecalMaterial()
        {
            if (_decalMaterial == null)
            {
                return null;
            }

            Material instance = new Material(_decalMaterial);
            Texture2D hole = PickRandomHole();
            if (hole == null)
            {
                return instance;
            }

            Texture2D tinted = GetTintedHole(hole);
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

            return instance;
        }

        private Texture2D GetTintedHole(Texture2D source)
        {
            if (source == null)
            {
                return null;
            }

            int id = source.GetInstanceID();
            if (_tintedHoles.TryGetValue(id, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D tinted = TintHole(source);
            _tintedHoles[id] = tinted;
            return tinted;
        }

        private Texture2D TintHole(Texture2D source)
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

            _tintBlitMaterial.SetColor("_Color", HoleColor);

            RenderTexture rt = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, rt, _tintBlitMaterial);

            Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, false)
            {
                name = source.name + "_HoleTint",
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
