using SS3D.Systems.Storage.Containers;
using SS3D.Systems.Storage.Items;
using System.Linq;
using SS3D.Systems.Roles;
using SS3D.Systems.Traits.TraitCategories;

namespace SS3D.Systems.Items
{
    /// <summary>
    /// What makes you a true Nanotrasen employee
    /// </summary>
    public class PDA : Item, IIdentification
    {
        public new void Awake()
        {
            base.Awake();

            Container = GetComponent<Container>();
        }

        public bool HasPermission(AccessPermission accessPermission)
        {
            if (accessPermission == null)
            {
                return true;
            }

            if (Container == null)
            {
                return false;
            }

            IDCard id = Container.Items.FirstOrDefault() as IDCard;
            
            return id != null && id.AccessPermissions.Contains(accessPermission);
        }
    }
}