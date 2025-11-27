using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Traits
{
    /// <summary>
    /// An inventory filter that only allows items with certain traits
    /// </summary>
    [CreateAssetMenu(fileName = "Filter", menuName = "Inventory/Filter")]
    public class Filter : ScriptableObject
    {
        [SerializeField]
        private bool _mustHaveAll;

        // Hash for identification
        private int _hash;

        [field:SerializeField]
        public List<Trait> AcceptedTraits { get; set; }

        [field:SerializeField]
        public List<Trait> DeniedTraits { get; set; }

        public bool CanStore(ITraitsHolder item)
        {
            int traitCount = 0;

            if (AcceptedTraits.Count == 0 && DeniedTraits.Count == 0)
            {
                return true;
            }

            foreach (Trait trait in item.Traits)
            {
                if (AcceptedTraits.Contains(trait))
                {
                    traitCount++;
                }
                else if (DeniedTraits.Contains(trait))
                {
                    return false;
                }
            }

            // If mustHaveAll then it will only return true if has all traits, otherwise having any will do
            if (_mustHaveAll)
            {
                return traitCount == AcceptedTraits.Count;
            }

            return traitCount > 0;
        }

        protected void OnValidate()
        {
            _hash = GetHash(name);
        }

        private static int GetHash([NotNull] string str) => Animator.StringToHash(str.ToUpper());
    }
}
