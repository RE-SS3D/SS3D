using FishNet.Object.Synchronizing;
using UnityEngine;

namespace SS3D.Systems.Traits.TraitCategories
{
    [CreateAssetMenu(fileName = "Access Permission", menuName = "Inventory/Traits/Permission")]
    public class AccessPermission : Trait, ICustomSync
    {
        public AccessPermission()
        {
            Category = TraitCategory.IDPermission;
        }

        public new object GetSerializedType()
        {
            return typeof(AccessPermission);
        }
    }
}
