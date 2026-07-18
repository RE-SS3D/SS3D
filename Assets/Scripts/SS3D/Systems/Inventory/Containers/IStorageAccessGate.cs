namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Optional gate on a container's parent (e.g. a locker door). When present, view/store
    /// interactions only succeed while <see cref="AllowsStorageAccess"/> is true.
    /// </summary>
    public interface IStorageAccessGate
    {
        bool AllowsStorageAccess { get; }
    }
}
