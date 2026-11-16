namespace SS3D.Networking
{
    /// <summary>
    /// Explicit session lifecycle for host/join. Owned by <see cref="ClientConnectionRecovery"/>.
    /// Offline is Boot only while <see cref="Cold"/>; after the first successful connect, offline
    /// is Empty so disconnect never reloads Intro.
    /// </summary>
    public enum SessionState
    {
        Cold = 0,
        Connecting = 1,
        Online = 2,
        Disconnecting = 3,
        WaitingForServer = 4,
    }
}
