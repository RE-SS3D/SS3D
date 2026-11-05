namespace SS3D.Systems.Audio
{
    /// <summary>
    /// A machine whose hum follows real state (audio.md §3). Read on clients, so implementations
    /// must answer from synced state.
    /// </summary>
    public interface IAmbientLoopState
    {
        bool IsLoopRunning { get; }
    }
}
