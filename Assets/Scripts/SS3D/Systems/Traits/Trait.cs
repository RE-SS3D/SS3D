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
        public int hash;
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
        public TraitCategories _category;
        public TraitCategories Category
        {
            get => _category;
        }

        protected bool Equals(Trait other)
        {
            // Use Hash instead of hash to prevent uninitialized hashes in clients
            return Hash == other.hash;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
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
