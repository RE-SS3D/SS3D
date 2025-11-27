using JetBrains.Annotations;
using UnityEngine;

namespace SS3D.Traits
{
    /// <summary>
    /// Used for checking certain characteristics in Items
    /// </summary>
    [CreateAssetMenu(fileName = "Trait", menuName = "Inventory/Traits/Trait")]
    public class Trait : ScriptableObject
    {
        // Hash used for identification
        private int _hash;

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

        [NotNull]
        public string Name
        {
            get => name;
            set => name = value;
        }

        // Categories, used for checking specific types of Traits
        public TraitCategories Category { get; set; }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return ReferenceEquals(this, other) || Equals((Trait)other);
        }

        public override int GetHashCode()
        {
            return Hash;
        }

        protected void OnValidate()
        {
            GenerateHash();
        }

        private void GenerateHash()
        {
            _hash = Animator.StringToHash(name.ToUpper());
        }

        // Two different object can have the same hash, it's usually a bad idea to test for equality with hash.
        private bool Equals([NotNull] Trait other) => Hash == other.Hash;
    }
}
