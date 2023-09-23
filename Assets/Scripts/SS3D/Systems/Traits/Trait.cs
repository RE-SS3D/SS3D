using SS3D.Systems.Traits;
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
        private int _hash;

        [SerializeField]
        private TraitCategories _category;

        public int Hash
        {
            get
            {
                if (_hash == 0)
                {
                    GenerateHash();
                }

                return _hash;
            }
            set => _hash = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        // Categories, used for checking specific types of Traits
        public TraitCategories Category
        {
            get => _category;
            set => _category = value;
        }

        public override int GetHashCode()
        {
            return _hash;
        }

        // Two different object can have the same hash, it's usually a bad idea to test for equality with hash.
        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((Trait)other);
        }

        protected void OnValidate()
        {
            GenerateHash();
        }

        private void GenerateHash()
        {
            _hash = Animator.StringToHash(name.ToUpper());
        }

        private bool Equals(Trait other)
        {
            // Use Hash instead of hash to prevent uninitialized hashes in clients
            return Hash == other._hash;
        }
    }
}
