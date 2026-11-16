using System;
using System.Collections.Generic;
using SS3D.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// A console command. Concrete subclasses are discovered by reflection in every loaded assembly
    /// (<see cref="CommandRegistry.DiscoverCommands"/>) and need a public parameterless constructor.
    /// Declare <see cref="Parameters"/> (or <see cref="Subcommands"/>); the registry parses, validates
    /// and writes help from them, so <see cref="Perform"/> only sees valid, typed values.
    /// </summary>
    public abstract class Command
    {
        protected static readonly IReadOnlyList<CommandParameter> NoParameters = Array.Empty<CommandParameter>();

        /// <summary>What players type; lowercase, unique.</summary>
        public abstract string Name { get; }

        public abstract CommandCategory Category { get; }

        /// <summary>One short sentence; help shows it after the signature. Don't repeat the usage here.</summary>
        public abstract string Description { get; }

        /// <summary>The lowest role allowed to run it. Ignored for <see cref="CommandType.Local"/> commands.</summary>
        public abstract ServerRoleTypes AccessLevel { get; }

        public abstract CommandType Type { get; }

        /// <summary>Arguments for a command without subcommands.</summary>
        public virtual IReadOnlyList<CommandParameter> Parameters => NoParameters;

        /// <summary>
        /// Modes with their own arguments; when non-null the first argument picks one and
        /// <see cref="Parameters"/> and <see cref="Perform"/> are unused. Cache the list in a field.
        /// </summary>
        public virtual IReadOnlyList<Subcommand> Subcommands => null;

        /// <summary>
        /// Example arguments (after the command name) shown by <c>help (command)</c>. An EditMode test parses
        /// every example, so a stale one fails. Null means no example; empty means the bare command.
        /// </summary>
        public virtual string Example => null;

        /// <summary>Run the command with arguments the registry already bound and validated.</summary>
        public virtual CommandResult Perform(CommandContext context, CommandArgs args) =>
            CommandResult.Error($"{Name} has subcommands; this should not be reached");

        public bool HasSubcommands => Subcommands != null && Subcommands.Count > 0;
    }
}
