namespace SS3D.Systems.IdAccess
{
    /// <summary>
    /// Device that records pass/fail auth checks per design/id-access.md §6.
    /// </summary>
    public interface IAuthLogDevice
    {
        string DeviceId { get; }

        DeviceAuthLog AuthLog { get; }
    }
}
