using System;
using UnityEngine;

namespace SS3D.Systems.Substances
{
    [CreateAssetMenu(menuName = "SS3D/Substances/Recipe Definition")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        [Serializable]
        public struct RecipeComponent
        {
            public string ReagentId;
            public float RelativeAmount;
        }

        [SerializeField] private string _id;
        [SerializeField] private RecipeComponent[] _ingredients = Array.Empty<RecipeComponent>();
        [SerializeField] private RecipeComponent[] _results = Array.Empty<RecipeComponent>();
        [SerializeField] private string _catalystReagentId;
        [SerializeField] private float _minimumTemperatureKelvin = float.NegativeInfinity;
        [SerializeField] private float _maximumTemperatureKelvin = float.PositiveInfinity;
        [SerializeField] private float _thermalDeltaKelvin;
        [SerializeField] private HazardKind _nearMissHazard = HazardKind.GasRelease;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;
        public RecipeComponent[] Ingredients => _ingredients;
        public RecipeComponent[] Results => _results;
        public string CatalystReagentId => _catalystReagentId;
        public float MinimumTemperatureKelvin => _minimumTemperatureKelvin;
        public float MaximumTemperatureKelvin => _maximumTemperatureKelvin;
        public float ThermalDeltaKelvin => _thermalDeltaKelvin;
        public HazardKind NearMissHazard => _nearMissHazard;

        public bool RequiresCatalyst => !string.IsNullOrEmpty(_catalystReagentId);

        public bool TemperatureSatisfied(float temperatureKelvin)
        {
            return temperatureKelvin >= _minimumTemperatureKelvin
                && temperatureKelvin <= _maximumTemperatureKelvin;
        }
    }
}
