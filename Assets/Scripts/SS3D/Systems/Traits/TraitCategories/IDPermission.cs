using SS3D.Systems.Traits;
using UnityEngine;

namespace SS3D.Systems
{
    [CreateAssetMenu(fileName = "ID Permission", menuName = "Inventory/Traits/Permission")]
    public class IDPermission : Trait
    {
        public IDPermission()
        {
            Category = TraitCategories.IDPermission;
        }
    }
}