using SS3D.Systems.Traits.TraitCategories;

namespace SS3D.Systems.Roles
{
    public interface IIdentification
    {
        public bool HasPermission(AccessPermission permission);
    }
}
