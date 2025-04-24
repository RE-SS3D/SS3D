using SS3D.Core.Behaviours;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Substances
{
    public sealed class SubstancesSubSystem : NetworkSubSystem
    {
        public Dictionary<SubstanceType, Substance> Substances => substances;
        public Recipe[] Recipes => recipes;

        // Only useful to serialize and fill the dictionnary substances.
        [SerializeField]
        private List<Substance> substancesList;


        private Dictionary<SubstanceType, Substance> substances;
        [SerializeField]

        private Recipe[] recipes;


        /// <summary>
        /// Gets a substance based on id
        /// </summary>
        /// <param name="id">The id name of the substance</param>
        /// <returns>A substance or null if it wasn't found</returns>
        public Substance FromType(SubstanceType type)
        {
            return Substances[type];
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            substances = new Dictionary<SubstanceType, Substance>();
            foreach (var substance in substancesList)
            {
                substances[substance.Type] = substance;
            }
            substancesList.Clear();

        }
    }

}