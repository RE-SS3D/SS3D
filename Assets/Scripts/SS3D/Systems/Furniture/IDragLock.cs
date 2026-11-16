namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// A component next to a <see cref="Draggable"/> that can forbid starting a drag, such as a bomb bolted to
    /// the floor. A drag already in progress is not cut short; the owner should refuse to lock while dragged.
    /// </summary>
    public interface IDragLock
    {
        bool DragLocked { get; }
    }
}
