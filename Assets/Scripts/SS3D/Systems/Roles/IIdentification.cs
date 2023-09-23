namespace SS3D.Systems.Roles
{
    /// <summary>
    /// Represents an item capable of holding ID Permissions
    /// </summary>
    public interface IIdentification
    {
        /// <summary>
        /// Check if the ID Card has the requested permission
        /// </summary>
        /// <param name="permission"></param>
        public bool HasPermission(IDPermission permission);
    }
}