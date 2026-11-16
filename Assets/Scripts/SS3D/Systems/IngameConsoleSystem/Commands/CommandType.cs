namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>Where a command runs once the caller's role has been checked.</summary>
    public enum CommandType
    {
        /// <summary>Runs on the server after the role check.</summary>
        Server,
        /// <summary>The server checks the role, then the calling client runs it.</summary>
        Client,
        /// <summary>Runs in this process with no server round trip and no role check (quit, harness assertions).</summary>
        Local,
    }
}
