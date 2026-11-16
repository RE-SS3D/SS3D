using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Entities;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>Who ran a command and from where. Built by <see cref="CommandsController"/>; tests build their own.</summary>
    public sealed class CommandContext
    {
        public CommandContext(NetworkConnection connection, string ckey, ServerRoleTypes role, Entity callerEntity, string commandLine)
        {
            Connection = connection;
            Ckey = ckey;
            Role = role;
            CallerEntity = callerEntity;
            CommandLine = commandLine;
        }

        /// <summary>The caller's connection on the server; null for Local and Client commands.</summary>
        public NetworkConnection Connection { get; }

        public string Ckey { get; }

        public ServerRoleTypes Role { get; }

        /// <summary>The body or ghost the caller controls, or null when they have none (lobby, or not resolvable on this side).</summary>
        public Entity CallerEntity { get; }

        /// <summary>The line as typed, for logging.</summary>
        public string CommandLine { get; }

        public bool HasEntity => CallerEntity != null;
    }
}
