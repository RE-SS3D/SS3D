using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Substances
{
    [CreateAssetMenu(menuName = "SS3D/Substances/Reagent Registry")]
    public sealed class ReagentRegistry : ScriptableObject
    {
        [SerializeField] private ReagentDefinition[] _definitions = Array.Empty<ReagentDefinition>();
        [SerializeField] private RecipeDefinition[] _recipes = Array.Empty<RecipeDefinition>();
        [SerializeField] private IncompatiblePair[] _incompatiblePairs = Array.Empty<IncompatiblePair>();

        private Dictionary<string, ReagentDefinition> _byId;

        [Serializable]
        public struct IncompatiblePair
        {
            public string ReagentIdA;
            public string ReagentIdB;
            public HazardKind Hazard;
        }

        public IReadOnlyList<ReagentDefinition> Definitions
        {
            get
            {
                Initialize();
                return _definitions;
            }
        }

        public IReadOnlyList<RecipeDefinition> Recipes
        {
            get
            {
                Initialize();
                return _recipes;
            }
        }

        public IReadOnlyList<IncompatiblePair> IncompatiblePairs
        {
            get
            {
                Initialize();
                return _incompatiblePairs;
            }
        }

        public void Initialize()
        {
            if (_byId != null)
            {
                return;
            }

            _byId = new Dictionary<string, ReagentDefinition>(StringComparer.Ordinal);
            if (_definitions == null)
            {
                return;
            }

            for (int i = 0; i < _definitions.Length; i++)
            {
                ReagentDefinition definition = _definitions[i];
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                {
                    continue;
                }

                _byId[definition.Id] = definition;
            }
        }

        public bool TryGet(string reagentId, out ReagentDefinition definition)
        {
            Initialize();
            if (string.IsNullOrEmpty(reagentId))
            {
                definition = null;
                return false;
            }

            return _byId.TryGetValue(reagentId, out definition);
        }

        public ReagentDefinition Get(string reagentId)
        {
            return TryGet(reagentId, out ReagentDefinition definition) ? definition : null;
        }
    }
}
