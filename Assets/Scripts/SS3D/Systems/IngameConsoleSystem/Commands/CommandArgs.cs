using System;
using System.Collections.Generic;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>Bound, typed argument values, looked up by parameter name.</summary>
    public sealed class CommandArgs
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _supplied = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<CommandTarget> _targets = new();

        public static readonly CommandArgs Empty = new();

        /// <summary>Every target this line resolved, for the admin action log.</summary>
        public IReadOnlyList<CommandTarget> Targets => _targets;

        internal void Set(string name, object value, bool supplied)
        {
            _values[name] = value;
            if (supplied)
            {
                _supplied.Add(name);
            }

            if (value is CommandTarget target)
            {
                _targets.Add(target);
            }
        }

        /// <summary>True when the caller typed this parameter (as opposed to it taking its default).</summary>
        public bool WasSupplied(string name) => _supplied.Contains(name);

        /// <summary>True when the parameter has a non-null value, typed or default.</summary>
        public bool Has(string name) => _values.TryGetValue(name, out object value) && value != null;

        public T Get<T>(string name)
        {
            if (!_values.TryGetValue(name, out object value))
            {
                throw new ArgumentException($"Command has no parameter named '{name}'", nameof(name));
            }

            return value == null ? default : (T)value;
        }

        public CommandTarget Target(string name = "target") => Get<CommandTarget>(name);

        public CommandItem Item(string name = "item") => Get<CommandItem>(name);

        public string Text(string name) => Get<string>(name);

        public float Float(string name) => Get<float>(name);

        public int Int(string name) => Get<int>(name);
    }
}
