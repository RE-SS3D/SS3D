namespace SS3D.Data.Persistence
{
    /// <summary>
    /// An object saved as a record of its own, such as an item. Stored in a container it is parented under its holder, so
    /// <see cref="ObjectSetup"/> stops at it: its setup travels in the container's contents, not as the holder's.
    /// </summary>
    public interface ISetupRecordRoot
    {
    }
}
