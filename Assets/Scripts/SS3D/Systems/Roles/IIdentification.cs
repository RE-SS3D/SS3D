using SS3D.Systems;
namespace SS3D.Systems.Roles
{
    /// <summary>
    /// Represents an item capable of holding ID Permissions
    /// </summary>
    public interface IIdentification
    {
        public bool HasPermission(IDPermission permission);
    }
}