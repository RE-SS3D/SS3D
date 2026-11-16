using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.IngameConsoleSystem.Commands;

namespace SS3D.Systems.IngameConsoleSystem
{
    /// <summary>
    /// Plain C# side of the console: command lookup, tokenizing, argument binding, help and suggestions.
    /// <see cref="CommandsController"/> wraps it with networking, the role check and the admin log;
    /// EditMode tests drive it directly with a fake <see cref="ICommandResolver"/>.
    /// </summary>
    public sealed class CommandRegistry
    {
        public const string HelpCommandName = "help";

        /// <summary>Widest signature column in the help list; longer signatures push their description over.</summary>
        private const int MaxSignatureColumn = 44;

        private const int MaxSuggestions = 3;

        private const int MaxSuggestionDistance = 2;

        private readonly Dictionary<string, Command> _commands = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _registrationProblems = new();

        public CommandRegistry(IEnumerable<Command> commands)
        {
            foreach (Command command in commands)
            {
                string name = command.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    _registrationProblems.Add($"{command.GetType().Name} has no Name");
                    continue;
                }

                if (_commands.TryGetValue(name, out Command existing))
                {
                    _registrationProblems.Add($"'{name}' is declared by both {existing.GetType().Name} and {command.GetType().Name}");
                    continue;
                }

                _commands.Add(name, command);
            }
        }

        public IEnumerable<Command> Commands => _commands.Values;

        /// <summary>Instantiates every concrete <see cref="Command"/> in every loaded assembly.</summary>
        /// <remarks>
        /// Every assembly, not just SS3D.Systems: UI assemblies (e.g. SS3D.UI.MainHud) host commands
        /// that cannot live in Systems without an asmdef cycle. Public top-level types only, so the
        /// private fake commands in the EditMode tests stay out of the real roster.
        /// </remarks>
        public static List<Command> DiscoverCommands()
        {
            List<Command> commands = new();
            Type commandBase = typeof(Command);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (Type type in types)
                {
                    if (type.IsClass && type.IsPublic && !type.IsAbstract && type.IsSubclassOf(commandBase)
                        && type.GetConstructor(Type.EmptyTypes) != null)
                    {
                        commands.Add((Command)Activator.CreateInstance(type));
                    }
                }
            }

            return commands;
        }

        public bool TryGet(string name, out Command command)
        {
            command = null;
            return !string.IsNullOrEmpty(name) && _commands.TryGetValue(name, out command);
        }

        /// <summary>A Local command runs for anyone; others need the role.</summary>
        public static bool CanRun(Command command, ServerRoleTypes role) =>
            command.Type == CommandType.Local || role >= command.AccessLevel;

        /// <summary>
        /// Split on whitespace (runs collapse); <c>"double quotes"</c> group words into one token.
        /// An unterminated quote runs to the end of the line.
        /// </summary>
        public static List<string> Tokenize(string line)
        {
            List<string> tokens = new();
            if (string.IsNullOrEmpty(line))
            {
                return tokens;
            }

            StringBuilder current = new();
            bool inQuotes = false;
            bool hasToken = false;
            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    hasToken = true;
                    continue;
                }

                if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (hasToken)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        hasToken = false;
                    }

                    continue;
                }

                current.Append(c);
                hasToken = true;
            }

            if (hasToken)
            {
                tokens.Add(current.ToString());
            }

            return tokens;
        }

        /// <summary>Split a line into a lowercase command name and its argument tokens. False for a blank line.</summary>
        public static bool TrySplitLine(string line, out string name, out string[] args)
        {
            List<string> tokens = Tokenize(line);
            if (tokens.Count == 0)
            {
                name = null;
                args = Array.Empty<string>();
                return false;
            }

            name = tokens[0].ToLowerInvariant();
            args = tokens.Skip(1).ToArray();
            return true;
        }

        /// <summary>
        /// Bind <paramref name="args"/> against the command's declaration. On failure <paramref name="error"/>
        /// is the problem alone (no prefix, no usage line).
        /// </summary>
        public bool TryBind(Command command, IReadOnlyList<string> args, CommandContext context, ICommandResolver resolver,
            out Subcommand subcommand, out CommandArgs bound, out string error)
        {
            subcommand = null;
            bound = null;
            IReadOnlyList<CommandParameter> parameters = command.Parameters;
            IReadOnlyList<string> remaining = args;

            if (command.HasSubcommands)
            {
                if (args.Count == 0)
                {
                    error = $"{command.Name} needs one of: {string.Join(", ", command.Subcommands.Select(s => s.Name))}";
                    return false;
                }

                subcommand = command.Subcommands.FirstOrDefault(s => string.Equals(s.Name, args[0], StringComparison.OrdinalIgnoreCase));
                if (subcommand == null)
                {
                    error = $"unknown {command.Name} mode '{args[0]}' (use {string.Join(", ", command.Subcommands.Select(s => s.Name))})";
                    return false;
                }

                parameters = subcommand.Parameters;
                remaining = args.Skip(1).ToArray();
            }

            return TryBindParameters(parameters, remaining, context, resolver, out bound, out error);
        }

        private static bool TryBindParameters(IReadOnlyList<CommandParameter> parameters, IReadOnlyList<string> tokens,
            CommandContext context, ICommandResolver resolver, out CommandArgs bound, out string error)
        {
            bound = new CommandArgs();
            error = null;

            // Arity first: with an optional target last, 'disk one two' should say "too many
            // arguments", not fail resolving 'one' as a ckey.
            if (parameters.All(p => p.Kind != ParameterKind.Rest) && tokens.Count > parameters.Count)
            {
                error = parameters.Count == 0
                    ? $"takes no arguments, got '{string.Join(" ", tokens)}'"
                    : $"too many arguments: '{string.Join(" ", tokens.Skip(parameters.Count))}'";
                return false;
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                CommandParameter parameter = parameters[i];

                if (parameter.Kind == ParameterKind.Rest)
                {
                    if (i < tokens.Count)
                    {
                        bound.Set(parameter.Name, string.Join(" ", tokens.Skip(i)), true);
                        break;
                    }
                }

                if (i >= tokens.Count)
                {
                    if (!parameter.IsOptional)
                    {
                        error = $"missing {parameter.Signature}";
                        return false;
                    }

                    bound.Set(parameter.Name, parameter.DefaultValue, false);
                    continue;
                }

                if (!TryParseToken(parameter, tokens[i], context, resolver, out object value, out error))
                {
                    return false;
                }

                bound.Set(parameter.Name, value, true);
            }

            return true;
        }

        private static bool TryParseToken(CommandParameter parameter, string token, CommandContext context,
            ICommandResolver resolver, out object value, out string error)
        {
            value = null;
            switch (parameter.Kind)
            {
                case ParameterKind.Target:
                    if (resolver == null)
                    {
                        error = "targets can't be resolved here";
                        return false;
                    }

                    if (!resolver.TryResolveTarget(token, context, out CommandTarget target, out error))
                    {
                        return false;
                    }

                    value = target;
                    return true;

                case ParameterKind.Item:
                    if (resolver == null)
                    {
                        error = "items can't be resolved here";
                        return false;
                    }

                    if (!resolver.TryResolveItem(token, out CommandItem item, out error))
                    {
                        return false;
                    }

                    value = item;
                    return true;

                default:
                    return parameter.TryParse(token, out value, out error);
            }
        }

        /// <summary>
        /// Bind and run. Never throws: a bad argument returns the problem plus the usage line, and an exception
        /// from the command is logged and returned as an error.
        /// </summary>
        public CommandResult Execute(Command command, IReadOnlyList<string> args, CommandContext context,
            ICommandResolver resolver, out CommandArgs bound)
        {
            bound = null;
            try
            {
                if (!TryBind(command, args, context, resolver, out Subcommand subcommand, out bound, out string error))
                {
                    return CommandResult.Failure(CommandResult.ErrorPrefix + error + "\n" + UsageText(command, subcommand));
                }

                return subcommand != null
                    ? subcommand.Perform(context, bound)
                    : command.Perform(context, bound);
            }
            catch (Exception e)
            {
                Log.Error(this, e, "Console command {line} threw", Logs.Generic, context?.CommandLine ?? command.Name);
                return CommandResult.Error(e.Message);
            }
        }

        /// <summary>One signature, e.g. <c>hurt &lt;target&gt; &lt;zone&gt; &lt;brute&gt; [burn=0]</c>.</summary>
        public static string Signature(Command command, Subcommand subcommand = null)
        {
            IReadOnlyList<CommandParameter> parameters = subcommand != null ? subcommand.Parameters : command.Parameters;
            StringBuilder builder = new(command.Name);
            if (subcommand != null)
            {
                builder.Append(' ').Append(subcommand.Name);
            }

            foreach (CommandParameter parameter in parameters)
            {
                builder.Append(' ').Append(parameter.Signature);
            }

            return builder.ToString();
        }

        /// <summary><c>usage:</c> lines for a command, or for one of its subcommands.</summary>
        public static string UsageText(Command command, Subcommand subcommand = null)
        {
            if (subcommand != null || !command.HasSubcommands)
            {
                return "usage: " + Signature(command, subcommand);
            }

            return string.Join("\n", command.Subcommands.Select(s => "usage: " + Signature(command, s)));
        }

        /// <summary>
        /// Help list lines for a command: one line, or for subcommands one line per mode with arguments
        /// plus one collapsed <c>name a|b</c> line for the modes without.
        /// </summary>
        private static IEnumerable<(string Signature, string Description)> ListLines(Command command)
        {
            if (!command.HasSubcommands)
            {
                yield return (Signature(command), command.Description);
                yield break;
            }

            List<Subcommand> bare = command.Subcommands.Where(s => s.Parameters.Count == 0).ToList();
            if (bare.Count > 0)
            {
                yield return ($"{command.Name} {string.Join("|", bare.Select(s => s.Name))}", command.Description);
            }

            bool first = bare.Count == 0;
            foreach (Subcommand subcommand in command.Subcommands.Where(s => s.Parameters.Count > 0))
            {
                // With no collapsed line, the first mode line carries the command's own description.
                string description = first ? command.Description : subcommand.Description;
                first = false;
                yield return (Signature(command, subcommand), description);
            }
        }

        /// <summary>
        /// The help list. <paramref name="filter"/>: null for Admin, Debug and Session; <c>all</c>; or a category name.
        /// Only commands <paramref name="role"/> can run are listed.
        /// </summary>
        public string Help(ServerRoleTypes role, string filter = null)
        {
            HashSet<CommandCategory> categories = new();
            if (string.IsNullOrEmpty(filter))
            {
                categories.Add(CommandCategory.Admin);
                categories.Add(CommandCategory.Debug);
                categories.Add(CommandCategory.Session);
            }
            else if (string.Equals(filter, "all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (CommandCategory category in Enum.GetValues(typeof(CommandCategory)))
                {
                    categories.Add(category);
                }
            }
            else if (TryParseCategory(filter, out CommandCategory single))
            {
                categories.Add(single);
            }

            List<(CommandCategory Category, string Signature, string Description)> lines = Commands
                .Where(c => categories.Contains(c.Category) && CanRun(c, role))
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Name, StringComparer.Ordinal)
                .SelectMany(c => ListLines(c).Select(l => (c.Category, l.Signature, l.Description)))
                .ToList();

            StringBuilder builder = new();
            builder.Append("help (command) for details · help test / help all for more");
            if (lines.Count == 0)
            {
                builder.Append("\nNo commands you can run here.");
                return builder.ToString();
            }

            int column = Math.Min(MaxSignatureColumn, lines.Max(l => l.Signature.Length)) + 2;
            CommandCategory? current = null;
            foreach ((CommandCategory category, string signature, string description) in lines)
            {
                if (current != category)
                {
                    builder.Append("\n\n").Append(category);
                    current = category;
                }

                builder.Append("\n  ").Append(signature);
                builder.Append(' ', Math.Max(2, column - signature.Length)).Append(description);
            }

            return builder.ToString();
        }

        /// <summary><c>help (command)</c>: description, every signature, value lists, the example and the role.</summary>
        public string HelpFor(Command command, ServerRoleTypes role)
        {
            StringBuilder builder = new();
            builder.Append(command.Name).Append(": ").Append(command.Description);

            List<CommandParameter> parameters = new(command.Parameters);
            if (command.HasSubcommands)
            {
                List<string> signatures = command.Subcommands.Select(s => Signature(command, s)).ToList();
                int column = signatures.Max(s => s.Length) + 2;
                for (int i = 0; i < command.Subcommands.Count; i++)
                {
                    Subcommand subcommand = command.Subcommands[i];
                    builder.Append("\n  ").Append(signatures[i]);
                    if (!string.IsNullOrEmpty(subcommand.Description))
                    {
                        builder.Append(' ', column - signatures[i].Length).Append(subcommand.Description);
                    }

                    parameters.AddRange(subcommand.Parameters);
                }
            }
            else
            {
                builder.Append("\n  ").Append(Signature(command));
            }

            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (CommandParameter parameter in parameters)
            {
                string values = parameter.ValuesLine;
                if (values != null && seen.Add(parameter.Name + "|" + values))
                {
                    builder.Append("\n  ").Append(values);
                }
            }

            if (command.Example != null)
            {
                builder.Append("\nexample: ").Append((command.Name + " " + command.Example).TrimEnd());
            }

            builder.Append("\nneeds: ").Append(command.Type == CommandType.Local ? "anyone (runs locally)" : command.AccessLevel.ToString());
            if (!CanRun(command, role))
            {
                builder.Append(" (you can't run this)");
            }

            return builder.ToString();
        }

        /// <summary>The answer to <c>help</c> with its argument tokens.</summary>
        public string HelpAnswer(IReadOnlyList<string> args, ServerRoleTypes role)
        {
            if (args.Count == 0)
            {
                return Help(role);
            }

            string topic = args[0];
            if (string.Equals(topic, "all", StringComparison.OrdinalIgnoreCase) || TryParseCategory(topic, out _))
            {
                return Help(role, topic);
            }

            return TryGet(topic, out Command command)
                ? HelpFor(command, role)
                : UnknownCommandText(topic, role);
        }

        /// <summary><c>Unknown command 'x'. Did you mean: y?</c></summary>
        public string UnknownCommandText(string name, ServerRoleTypes role)
        {
            IReadOnlyList<string> suggestions = Suggest(name, role);
            string text = $"Unknown command '{name}'.";
            return suggestions.Count > 0
                ? text + $" Did you mean: {string.Join(", ", suggestions)}?"
                : text + " Type help for a list.";
        }

        /// <summary>Commands <paramref name="role"/> can run whose name starts with, or is within 2 edits of, <paramref name="name"/>.</summary>
        public IReadOnlyList<string> Suggest(string name, ServerRoleTypes role)
        {
            if (string.IsNullOrEmpty(name))
            {
                return Array.Empty<string>();
            }

            string lowered = name.ToLowerInvariant();
            return Commands
                .Where(c => CanRun(c, role))
                .Select(c => (c.Name, Distance: c.Name.StartsWith(lowered, StringComparison.Ordinal) ? 0 : EditDistance(lowered, c.Name)))
                .Where(c => c.Distance <= MaxSuggestionDistance)
                .OrderBy(c => c.Distance)
                .ThenBy(c => c.Name, StringComparer.Ordinal)
                .Take(MaxSuggestions)
                .Select(c => c.Name)
                .ToList();
        }

        /// <summary>
        /// Declaration problems: duplicate or non-lowercase names, a Rest or optional Target that isn't last,
        /// a required parameter after an optional one, missing descriptions. An EditMode test asserts this is empty.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            List<string> problems = new(_registrationProblems);
            foreach (Command command in Commands)
            {
                string name = command.Name;
                if (name != name.ToLowerInvariant() || name.Any(char.IsWhiteSpace))
                {
                    problems.Add($"'{name}' must be lowercase with no spaces");
                }

                if (name == HelpCommandName)
                {
                    problems.Add($"{command.GetType().Name} can't be named '{HelpCommandName}'");
                }

                if (string.IsNullOrWhiteSpace(command.Description))
                {
                    problems.Add($"'{name}' has no Description");
                }

                if (command.HasSubcommands)
                {
                    if (command.Parameters.Count > 0)
                    {
                        problems.Add($"'{name}' declares both Parameters and Subcommands");
                    }

                    HashSet<string> modes = new();
                    foreach (Subcommand subcommand in command.Subcommands)
                    {
                        if (subcommand.Name != subcommand.Name.ToLowerInvariant() || !modes.Add(subcommand.Name))
                        {
                            problems.Add($"'{name} {subcommand.Name}' must be lowercase and unique");
                        }

                        if (subcommand.Perform == null)
                        {
                            problems.Add($"'{name} {subcommand.Name}' has no Perform");
                        }

                        ValidateParameters($"{name} {subcommand.Name}", subcommand.Parameters, problems);
                    }
                }
                else
                {
                    ValidateParameters(name, command.Parameters, problems);
                }
            }

            return problems;
        }

        private static void ValidateParameters(string owner, IReadOnlyList<CommandParameter> parameters, List<string> problems)
        {
            bool sawOptional = false;
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < parameters.Count; i++)
            {
                CommandParameter parameter = parameters[i];
                bool last = i == parameters.Count - 1;

                if (!names.Add(parameter.Name))
                {
                    problems.Add($"'{owner}' declares parameter '{parameter.Name}' twice");
                }

                if (parameter.Kind == ParameterKind.Rest && !last)
                {
                    problems.Add($"'{owner}': rest parameter '{parameter.Name}' must be last");
                }

                if (parameter.Kind == ParameterKind.Target && parameter.IsOptional && !last)
                {
                    problems.Add($"'{owner}': optional target '{parameter.Name}' must be last");
                }

                if (!parameter.IsOptional && sawOptional)
                {
                    problems.Add($"'{owner}': required '{parameter.Name}' follows an optional parameter");
                }

                if ((parameter.Kind == ParameterKind.Choice || parameter.Kind == ParameterKind.Enum) && parameter.Choices.Count == 0)
                {
                    problems.Add($"'{owner}': '{parameter.Name}' has no values");
                }

                sawOptional |= parameter.IsOptional;
            }
        }

        private static bool TryParseCategory(string text, out CommandCategory category)
        {
            foreach (CommandCategory value in Enum.GetValues(typeof(CommandCategory)))
            {
                if (string.Equals(value.ToString(), text, StringComparison.OrdinalIgnoreCase))
                {
                    category = value;
                    return true;
                }
            }

            category = default;
            return false;
        }

        private static int EditDistance(string a, string b)
        {
            int[] previous = new int[b.Length + 1];
            int[] current = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++)
            {
                previous[j] = j;
            }

            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }

                (previous, current) = (current, previous);
            }

            return previous[b.Length];
        }
    }
}
