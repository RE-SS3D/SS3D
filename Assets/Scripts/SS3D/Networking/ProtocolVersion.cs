namespace SS3D.Networking
{
    /// <summary>
    /// Wire-compatibility number checked by <see cref="ProtocolVersionAuthenticator"/> before a
    /// client may talk to the server. Bump <see cref="Current"/> by hand whenever a change means
    /// a build can no longer talk to the previous one (new/renamed NetworkBehaviours or SyncVars,
    /// changed RPC/broadcast signatures, FishNet upgrades). A bump also forces a MINOR release —
    /// see Documents/architecture/2026-10_game-versioning.md.
    /// </summary>
    public static class ProtocolVersion
    {
        public const int Current = 31;

        /// <summary>Human-readable message for the client UI, e.g. "Server 0.2.0 (protocol 2), you have 0.1.4 (protocol 1)".</summary>
        public static string Describe(string serverVersion, int serverProtocol, string clientVersion, int clientProtocol) =>
            $"Version mismatch: server is {serverVersion} (protocol {serverProtocol}), you have {clientVersion} (protocol {clientProtocol}).";
    }
}
