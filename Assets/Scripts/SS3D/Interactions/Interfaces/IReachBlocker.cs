namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// A world object that a hand cannot reach through, such as a wall, window or closed door.
    /// <see cref="Extensions.InteractionExtensions.RangeCheck"/> fails when the segment from the
    /// interaction origin to the target crosses a collider whose parent chain has a blocker that
    /// returns true here, unless that blocker is the target itself.
    /// </summary>
    public interface IReachBlocker
    {
        bool BlocksReach { get; }
    }
}
