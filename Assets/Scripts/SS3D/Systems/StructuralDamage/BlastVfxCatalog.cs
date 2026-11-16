using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Shared blast explosion VFX assets and tuning, loaded from Resources.
    /// </summary>
    public sealed class BlastVfxCatalog : ScriptableObject
    {
        public static readonly Color FireCoreColor = new(1f, 0.55f, 0.15f, 1f);

        [SerializeField] private GameObject _burstPrefab;
        [SerializeField] private GameObject _scorchFloorPrefab;
        [SerializeField] private Material _scorchDecalMaterial;
        [SerializeField] private Material _particleMaterial;

        [Header("Falloff")]
        [SerializeField] private float _shakeFullRange = 6f;
        [SerializeField] private float _shakeMaxRange = 24f;
        [SerializeField] private float _flashFullRange = 4f;
        [SerializeField] private float _flashMaxRange = 20f;

        [Header("Shake")]
        [SerializeField] private float _shakeAmplitude = 0.75f;
        [SerializeField] private float _shakeDuration = 0.85f;

        [Header("Light")]
        [SerializeField] private float _lightIntensity = 35f;
        [SerializeField] private float _lightRange = 14f;
        [SerializeField] private float _lightDuration = 0.4f;

        [Header("Lifetime")]
        [SerializeField] private float _effectLifetime = 1.2f;

        private static BlastVfxCatalog _instance;

        public static BlastVfxCatalog Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<BlastVfxCatalog>(nameof(BlastVfxCatalog));

                return _instance;
            }
        }

        public GameObject BurstPrefab => _burstPrefab;
        public GameObject ScorchFloorPrefab => _scorchFloorPrefab;
        public Material ScorchDecalMaterial => _scorchDecalMaterial;
        public Material ParticleMaterial => _particleMaterial;

        public float ShakeFullRange => _shakeFullRange;
        public float ShakeMaxRange => _shakeMaxRange;
        public float FlashFullRange => _flashFullRange;
        public float FlashMaxRange => _flashMaxRange;
        public float ShakeAmplitude => _shakeAmplitude;
        public float ShakeDuration => _shakeDuration;
        public float LightIntensity => _lightIntensity;
        public float LightRange => _lightRange;
        public float LightDuration => _lightDuration;
        public float EffectLifetime => _effectLifetime;
    }
}
