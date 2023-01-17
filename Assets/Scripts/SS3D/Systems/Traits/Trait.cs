using UnityEngine;

namespace SS3D.Systems
{
    [CreateAssetMenu(fileName = "Trait", menuName = "Inventory/Traits/Trait")]
    public class Trait : ScriptableObject
    {
        //Hash for identification
        protected int hash;
        [HideInInspector]
        public int Hash
        {
            get => hash;
            set => hash = value;
        }

        protected TraitCategory category;
        [HideInInspector]
        public TraitCategory Category
        {
            get => category;
        }

        protected bool Equals(Trait other)
        {
            return hash == other.hash;
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
            hash = Animator.StringToHash(name.ToUpper());
        }
    }

}
