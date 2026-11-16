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
        [SerializeField] private SS3D.Systems.Substances.ReagentDefinition _linkedReagent;
        [SerializeField] private GasVisualProfile _visualProfile;
        [SerializeField] private string _symbol;
        [SerializeField] private Color32 _panelColor = new(138, 141, 146, 255);
        [SerializeField] private bool _toxic;

        public ushort Id => _id;
        public string DisplayName => _displayName;
        public float MolarMass => _molarMass;
        public float SpecificHeat => _specificHeat;
        public SS3D.Systems.Substances.ReagentDefinition LinkedReagent => _linkedReagent;
        public GasVisualProfile VisualProfile => _visualProfile;

        /// <summary>Short name on machine panels, such as CO2.</summary>
        public string Symbol => string.IsNullOrEmpty(_symbol) ? _displayName : _symbol;

        /// <summary>This gas's swatch and bar colour on machine panels.</summary>
        public Color32 PanelColor => _panelColor;

        /// <summary>Scrubbers' Toxins setting takes every gas with this set.</summary>
        public bool IsToxic => _toxic;

        public GasId GasId => new(_id);
    }
}
