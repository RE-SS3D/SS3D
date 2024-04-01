using UnityEngine;

namespace SS3D.Systems
{
    /// <summary>
    /// Used for checking certain characteristics in Items
    /// </summary>
    [CreateAssetMenu(fileName = "Trait", menuName = "Inventory/Traits/Trait")]
    public class Trait : ScriptableObject
    {
        // Hash used for identification
        protected int hash;
        public int Hash
        {
            get
            {
                if (hash == 0)
                {
                    GenerateHash();
                }

                return hash;
            }
            set => hash = value;
        }

        [HideInInspector]
        public string Name
        {
            get => name;
            set => name = value;
        }

        // Categories, used for checking specific types of Traits
        protected TraitCategories _category;
        public TraitCategories Category
        {
            get => _category;
            set => _category = value;
        }

        //Two different object can have the same hash, it's usually a bad idea to test for equality with hash.
        protected bool Equals(Trait other)
        {
            // Use Hash instead of hash to prevent uninitialized hashes in clients
            return Hash == other.hash;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return Equals((Trait)obj);
        }

        public override int GetHashCode()
        {
            return hash;
        }

        [ExecuteInEditMode]
        private void OnValidate()
        {
            GenerateHash();
        }

        private void GenerateHash()
        {
            hash = Animator.StringToHash(name.ToUpper());
        }
    }

}
