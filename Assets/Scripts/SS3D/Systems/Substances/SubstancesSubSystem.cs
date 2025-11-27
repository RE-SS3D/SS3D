using SS3D.Core.Behaviours;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Substances
{
    public sealed class SubstancesSubSystem : NetworkSubSystem
    {
        // Only useful to serialize and fill the dictionnary substances.
        [SerializeField]
        private List<Substance> _substancesList;

        public Dictionary<SubstanceType, Substance> Substances { get; private set; }

        [field:SerializeField]
        public Recipe[] Recipes { get; private set; }

        /// <summary>
        /// Gets a substance based on id
        /// </summary>
        /// <param name="type">The id name of the substance</param>
        /// <returns>A substance or null if it wasn't found</returns>
        public Substance FromType(SubstanceType type) => Substances[type];

        protected override void OnAwake()
        {
            base.OnAwake();
            Substances = new();
            foreach (Substance substance in _substancesList)
            {
                Substances[substance.Type] = substance;
            }

            _substancesList.Clear();
        }
    }
}
