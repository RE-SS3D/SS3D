using FishNet.Object.Synchronizing;
using SS3D.Systems.Traits.TraitCategories;
using UnityEngine;

namespace SS3D.Systems.Traits
{
    [CreateAssetMenu(fileName = "Trait", menuName = "Inventory/Traits/Trait")]
    public class Trait : ScriptableObject, ICustomSync
    {
        //Hash for identification
        protected int hash;
        [HideInInspector]
        public int Hash
        {
            get => hash;
            set => hash = value;
        }

        protected TraitCategory Category;

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

        public object GetSerializedType()
        {
            return typeof(Trait);
        }

        [ExecuteInEditMode]
        private void OnValidate()
        {
            hash = Animator.StringToHash(name.ToUpper());
        }
    }

}
