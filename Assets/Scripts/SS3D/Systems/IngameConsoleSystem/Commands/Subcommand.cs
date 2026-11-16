using System;
using System.Collections.Generic;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// One mode of a command with its own parameters, e.g. <c>atmos gas (gas) (moles)</c>.
    /// The first token after the command name picks it, case-insensitively.
    /// </summary>
    public sealed class Subcommand
    {
        public Subcommand(string name, string description, Func<CommandContext, CommandArgs, CommandResult> perform,
            params CommandParameter[] parameters)
        {
            Name = name;
            Description = description;
            Perform = perform;
            Parameters = parameters ?? Array.Empty<CommandParameter>();
        }

        public string Name { get; }

        /// <summary>One short sentence for <c>help (command)</c>.</summary>
        public string Description { get; }

        public Func<CommandContext, CommandArgs, CommandResult> Perform { get; }

        public IReadOnlyList<CommandParameter> Parameters { get; }
    }
}
