using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    [CreateAssetMenu(menuName = "SS3D/Atmospherics/Gas Registry")]
    public sealed class GasRegistry : ScriptableObject
    {
        [SerializeField] private GasDefinition[] _definitions = Array.Empty<GasDefinition>();

        private GasDefinition[] _sortedDefinitions;
        private Dictionary<ushort, GasDefinition> _byId;

        public int Count => _sortedDefinitions?.Length ?? 0;

        public void Initialize()
        {
            if (_sortedDefinitions != null)
                return;

            var valid = new List<GasDefinition>();
            foreach (GasDefinition definition in _definitions)
            {
                if (definition != null)
                    valid.Add(definition);
            }

            valid.Sort((a, b) => a.Id.CompareTo(b.Id));
            _sortedDefinitions = valid.ToArray();
            _byId = new Dictionary<ushort, GasDefinition>(_sortedDefinitions.Length);

            foreach (GasDefinition definition in _sortedDefinitions)
            {
                if (definition.Id >= AtmosConstants.MaxGasTypes)
                {
                    Debug.LogError($"Gas {definition.DisplayName} id {definition.Id} exceeds MaxGasTypes ({AtmosConstants.MaxGasTypes}).");
                    continue;
                }

                if (_byId.ContainsKey(definition.Id))
                {
                    Debug.LogError($"Duplicate gas id {definition.Id} for {definition.DisplayName}.");
                    continue;
                }

                _byId[definition.Id] = definition;
            }
        }

        public bool TryGetDefinition(GasId gasId, out GasDefinition definition)
        {
            Initialize();
            return _byId.TryGetValue(gasId.Value, out definition);
        }

        public GasDefinition GetDefinition(GasId gasId)
        {
            TryGetDefinition(gasId, out GasDefinition definition);
            return definition;
        }

        public IReadOnlyList<GasDefinition> Definitions
        {
            get
            {
                Initialize();
                return _sortedDefinitions;
            }
        }
    }
}
