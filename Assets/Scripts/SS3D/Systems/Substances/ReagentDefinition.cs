using UnityEngine;

namespace SS3D.Systems.Substances
{
    [CreateAssetMenu(menuName = "SS3D/Substances/Reagent Definition")]
    public sealed class ReagentDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private ReagentCategory _category = ReagentCategory.Industrial;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private float _molarMass = 18f;
        [SerializeField] private float _millilitersPerMole = 18f;
        [SerializeField] private float _boilingPointKelvin = 373.15f;
        [SerializeField] private float _freezingPointKelvin = 273.15f;
        [SerializeField] private float _flashPointKelvin = float.PositiveInfinity;
        [SerializeField] private float _metabolismRate = 1f;
        [SerializeField] private float _overdoseThresholdMl = 50f;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public ReagentCategory Category => _category;
        public Color Color => _color;
        public float MolarMass => _molarMass;
        public float MillilitersPerMole => _millilitersPerMole;
        public float BoilingPointKelvin => _boilingPointKelvin;
        public float FreezingPointKelvin => _freezingPointKelvin;
        public float FlashPointKelvin =>
            _flashPointKelvin >= 1e30f ? float.PositiveInfinity : _flashPointKelvin;
        public float MetabolismRate => _metabolismRate;
        public float OverdoseThresholdMl => _overdoseThresholdMl;

        public float VolumeMlToMoles(float volumeMl)
        {
            if (_millilitersPerMole <= 0f)
            {
                return 0f;
            }

            return volumeMl / _millilitersPerMole;
        }

        public float MolesToVolumeMl(float moles) => moles * _millilitersPerMole;
    }
}
