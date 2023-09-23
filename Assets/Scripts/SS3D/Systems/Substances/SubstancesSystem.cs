using SS3D.Core.Behaviours;
using SS3D.Substances;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Substances
{
    public sealed class SubstancesSystem : NetworkSystem
    {
        // Only useful to serialize and fill the dictionnary substances.
        [SerializeField]
        private List<Substance> _substancesList;

        private Dictionary<SubstanceType, Substance> _substances;

        [SerializeField]
        private Recipe[] _recipes;

        public Dictionary<SubstanceType, Substance> Substances => _substances;

        public Recipe[] Recipes => _recipes;

        /// <summary>
        /// Gets a substance based on id
        /// </summary>
        /// <returns>A substance or null if it wasn't found</returns>
        public Substance FromType(SubstanceType type)
        {
            return Substances[type];
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            _substances = new Dictionary<SubstanceType, Substance>();
            foreach (Substance substance in _substancesList)
            {
                _substances[substance.Type] = substance;
            }

            _substancesList.Clear();
        }
    }
}