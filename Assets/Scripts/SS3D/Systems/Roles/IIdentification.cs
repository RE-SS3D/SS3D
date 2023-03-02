using SS3D.Systems;
/// <summary>
/// Represents an item capable of holding ID Permissions
/// </summary>
namespace SS3D.Systems.Roles
{
    public interface IIdentification
    {
        public bool HasPermission(IDPermission permission);
    }
}