namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// How <c>help</c> groups a command. Only presentation: who may run it is <see cref="Command.AccessLevel"/>.
    /// Declaration order is the order help prints the groups in.
    /// </summary>
    public enum CommandCategory
    {
        /// <summary>Playtest admin tools: health, spawning, the disk, round controls, roles.</summary>
        Admin,
        /// <summary>Developer debugging: blasts, atmos, doors, lights, local visual overrides.</summary>
        Debug,
        /// <summary>Process and connection: quit, reconnect.</summary>
        Session,
        /// <summary>Harness assertions; hidden from plain <c>help</c>.</summary>
        Test,
    }
}
