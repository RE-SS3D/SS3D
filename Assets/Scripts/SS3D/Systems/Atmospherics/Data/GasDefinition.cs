using SS3D.Substances;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    [CreateAssetMenu(menuName = "SS3D/Atmospherics/Gas Definition")]
    public sealed class GasDefinition : ScriptableObject
    {
        [SerializeField] private ushort _id;
        [SerializeField] private string _displayName;
        [SerializeField] private float _molarMass = 32f;
        [SerializeField] private float _specificHeat = 20f;
        [SerializeField] private Substance _linkedSubstance;

        public ushort Id => _id;
        public string DisplayName => _displayName;
        public float MolarMass => _molarMass;
        public float SpecificHeat => _specificHeat;
        public Substance LinkedSubstance => _linkedSubstance;

        public GasId GasId => new(_id);
    }
}
