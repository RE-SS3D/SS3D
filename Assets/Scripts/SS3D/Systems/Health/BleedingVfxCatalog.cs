using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Shared blood VFX assets (splatter textures, particle material, decal material) loaded from Resources.
    /// </summary>
    public sealed class BleedingVfxCatalog : ScriptableObject
    {
        [SerializeField] private Texture2D[] _splatters;
        [SerializeField] private Material _particleMaterial;
        [SerializeField] private Material _bloodDecalMaterial;
        [SerializeField] private GameObject _floorDecalPrefab;

        private static BleedingVfxCatalog _instance;

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
                if (instance.HasProperty("Base_Map"))
                {
                    instance.SetTexture("Base_Map", splatter);
                }

                if (instance.HasProperty("_BaseMap"))
                {
                    instance.SetTexture("_BaseMap", splatter);
                }

                if (instance.HasProperty("_MainTex"))
                {
                    instance.SetTexture("_MainTex", splatter);
                }
            }

            return instance;
        }
    }
}
