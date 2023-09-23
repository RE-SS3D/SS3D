using System.Collections.Generic;
using UnityEngine;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems
{
    /// <summary>
    /// An inventory filter that only allows items with certain traits
    /// </summary>
    [CreateAssetMenu(fileName = "Filter", menuName = "Inventory/Filter")]
    public class Filter : ScriptableObject
    {
        public bool MustHaveAll;
        public List<Trait> AcceptedTraits;
        public List<Trait> DeniedTraits;

        private int _hash;

        public int Hash => _hash;

        public static int GetHash(string str)
        {
            return Animator.StringToHash(str.ToUpper());
        }

        public bool CanStore(Item item)
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
            if (MustHaveAll)
            {
                return traitCount == AcceptedTraits.Count;
            }

            return traitCount > 0;
        }

        protected void OnValidate()
        {
            _hash = GetHash(name);
        }
    }
}