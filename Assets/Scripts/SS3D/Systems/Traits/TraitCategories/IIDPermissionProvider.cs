namespace SS3D.Traits
{
    public interface IIDPermissionProvider
    {
        public bool HasPermission(IDPermission permission);
    }
}
