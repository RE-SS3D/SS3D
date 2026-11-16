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

        /// <summary>
        /// Number of SoA gas slots the simulation must iterate (highest id + 1), capped at
        /// <see cref="AtmosConstants.MaxGasTypes"/>.
        /// </summary>
        public int GetSlotCount()
        {
            Initialize();

            int slots = GasDefaults.CoreGasCount;
            foreach (GasDefinition definition in _sortedDefinitions)
                slots = Mathf.Max(slots, definition.Id + 1);

            return Mathf.Clamp(slots, 1, AtmosConstants.MaxGasTypes);
        }

        public void Initialize()
        {
            // Require both caches. Play Mode without domain reload (and hub unload/reload) can
            // leave the array while dropping the dictionary — early-return on array alone NREs
            // at TryGetDefinition after AtmosSubSystem.GetSlotCount already succeeded.
            if (_sortedDefinitions != null && _byId != null)
                return;

            var valid = new List<GasDefinition>();
            if (_definitions != null)
            {
                foreach (GasDefinition definition in _definitions)
                {
                    if (definition != null)
                        valid.Add(definition);
                }
            }

            valid.Sort((a, b) => a.Id.CompareTo(b.Id));
            GasDefinition[] sorted = valid.ToArray();
            var byId = new Dictionary<ushort, GasDefinition>(sorted.Length);

            foreach (GasDefinition definition in sorted)
            {
                if (definition.Id >= AtmosConstants.MaxGasTypes)
                {
                    Debug.LogError($"Gas {definition.DisplayName} id {definition.Id} exceeds MaxGasTypes ({AtmosConstants.MaxGasTypes}).");
                    continue;
                }

                if (byId.ContainsKey(definition.Id))
                {
                    Debug.LogError($"Duplicate gas id {definition.Id} for {definition.DisplayName}.");
                    continue;
                }

                byId[definition.Id] = definition;
            }

            // Assign only after both are fully built so a re-entrant/failed init cannot early-return
            // with a half-ready registry.
            _byId = byId;
            _sortedDefinitions = sorted;
        }

        public bool TryGetDefinition(GasId gasId, out GasDefinition definition)
        {
            Initialize();
            if (_byId == null)
            {
                definition = null;
                return false;
            }

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
