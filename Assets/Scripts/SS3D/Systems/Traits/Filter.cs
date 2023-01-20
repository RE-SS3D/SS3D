using System.Collections.Generic;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Traits
{
    [CreateAssetMenu(fileName = "Filter", menuName = "Inventory/Filter")]
    public class Filter : ScriptableObject
    {
        public bool mustHaveAll;
        public List<Trait> acceptedTraits;
        public List<Trait> deniedTraits;

        public bool CanStore(Item item)
        {
            int traitCount = 0;
            if (acceptedTraits.Count == 0 && deniedTraits.Count == 0)
                return true;

            foreach (Trait trait in item.traits)
            {
                if (acceptedTraits.Contains(trait))
                {
                    traitCount++;
                }
                else if (deniedTraits.Contains(trait))
                {
                    return false;
                }
            }

            //If mustHaveAll then it will only return true if has all traits, otherwise having any will do
            if (mustHaveAll)
            {
                return traitCount == acceptedTraits.Count;
            }
            else
            {
                return traitCount > 0;
            }
        }
        
        //Hash for identification
        protected int Hash;

        [ExecuteInEditMode]
        private void OnValidate()
        {
            Hash = GetHash(name);
        }

        public static int GetHash(string str)
        {
            return Animator.StringToHash(str.ToUpper());
        }
    }
}