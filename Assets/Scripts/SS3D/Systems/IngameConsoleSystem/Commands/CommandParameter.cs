using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public enum ParameterKind
    {
        /// <summary><c>me</c>, a ckey or <c>nearest</c>, resolved by <see cref="ICommandResolver"/>.</summary>
        Target,
        Int,
        Float,
        /// <summary>One of an enum's defined names, case-insensitive.</summary>
        Enum,
        /// <summary>One of a fixed list of words, case-insensitive. Parses to the canonical lowercase word.</summary>
        Choice,
        /// <summary>An item asset id or name, resolved by <see cref="ICommandResolver"/>.</summary>
        Item,
        /// <summary>One free-form token (a ckey, a profile id, an access level).</summary>
        Text,
        /// <summary>Every remaining token, joined with spaces. Only valid as the last parameter.</summary>
        Rest,
    }

    /// <summary>
    /// One declared argument. Build with <see cref="P"/>; parsing, validation, help and usage lines
    /// are all generated from these, so a signature cannot drift from what the parser accepts.
    /// </summary>
    public sealed class CommandParameter
    {
        /// <summary>Choices with more values than this show as &lt;name&gt; in the help list.</summary>
        public const int MaxInlineChoices = 4;

        private string[] _choices = Array.Empty<string>();

        internal CommandParameter(string name, ParameterKind kind)
        {
            Name = name;
            Kind = kind;
        }

        public string Name { get; }

        public ParameterKind Kind { get; }

        public bool IsOptional { get; private set; }

        /// <summary>The value an omitted optional parameter takes (may be null).</summary>
        public object DefaultValue { get; private set; }

        /// <summary>How the default reads in help, e.g. <c>full</c> for a large heal amount.</summary>
        public string DefaultLabel { get; private set; }

        public double? Min { get; private set; }

        public double? Max { get; private set; }

        public Type EnumType { get; private set; }

        /// <summary>Allowed words for <see cref="ParameterKind.Choice"/>, or the enum's allowed names.</summary>
        public IReadOnlyList<string> Choices => _choices;

        /// <summary>Make the parameter optional with no default; an omitted value reads as null.</summary>
        public CommandParameter Optional()
        {
            IsOptional = true;
            return this;
        }

        /// <summary>Make the parameter optional with a default value.</summary>
        public CommandParameter Default(object value, string label = null)
        {
            IsOptional = true;
            // Store the type the binder produces, so Default(120) on a Float reads back as 120f.
            DefaultValue = Kind switch
            {
                ParameterKind.Float when value is IConvertible convertible => convertible.ToSingle(CultureInfo.InvariantCulture),
                ParameterKind.Int when value is IConvertible convertible => convertible.ToInt32(CultureInfo.InvariantCulture),
                _ => value,
            };
            DefaultLabel = label ?? FormatValue(DefaultValue);
            return this;
        }

        public CommandParameter Range(double min, double max)
        {
            Min = min;
            Max = max;
            return this;
        }

        internal CommandParameter WithChoices(IEnumerable<string> choices)
        {
            _choices = choices.ToArray();
            return this;
        }

        internal CommandParameter WithEnum(Type enumType, IEnumerable<string> names)
        {
            EnumType = enumType;
            _choices = names.ToArray();
            return this;
        }

        /// <summary>
        /// Help notation: <c>&lt;name&gt;</c> required, <c>[name=default]</c> optional, and choices of up to
        /// <see cref="MaxInlineChoices"/> values inline as <c>a|b|c</c> (wrapped in [] when optional).
        /// </summary>
        public string Signature
        {
            get
            {
                bool inlineChoices = Kind == ParameterKind.Choice && _choices.Length <= MaxInlineChoices;
                if (inlineChoices)
                {
                    string joined = string.Join("|", _choices);
                    return IsOptional ? $"[{joined}]" : joined;
                }

                string name = Kind == ParameterKind.Rest ? Name + "..." : Name;
                if (!IsOptional)
                {
                    return $"<{name}>";
                }

                return string.IsNullOrEmpty(DefaultLabel) ? $"[{name}]" : $"[{name}={DefaultLabel}]";
            }
        }

        /// <summary>The line <c>help (command)</c> prints for an enum or long choice; null when the signature already says it all.</summary>
        public string ValuesLine
        {
            get
            {
                switch (Kind)
                {
                    case ParameterKind.Enum:
                    case ParameterKind.Choice when _choices.Length > MaxInlineChoices:
                        return $"{Name}: {string.Join(", ", _choices)}";
                    case ParameterKind.Choice when IsOptional && !string.IsNullOrEmpty(DefaultLabel):
                        return $"{Name}: {string.Join(", ", _choices)} (default {DefaultLabel})";
                    case ParameterKind.Target:
                        return $"{Name}: me, a ckey, or nearest (closest body within {CommandResolver.NearestRange:0} m)";
                    case ParameterKind.Int:
                    case ParameterKind.Float:
                        return Min.HasValue && Max.HasValue
                            ? $"{Name}: {FormatValue(Min.Value)} to {FormatValue(Max.Value)}"
                            : null;
                    default:
                        return null;
                }
            }
        }

        /// <summary>Parse one token for every kind except Target, Item and Rest, which the registry binds.</summary>
        internal bool TryParse(string token, out object value, out string error)
        {
            value = null;
            error = null;
            switch (Kind)
            {
                case ParameterKind.Int:
                    if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                    {
                        error = $"{Name} must be a whole number, got '{token}'";
                        return false;
                    }

                    if (!InRange(i, out error))
                    {
                        return false;
                    }

                    value = i;
                    return true;

                case ParameterKind.Float:
                    if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)
                        || float.IsNaN(f) || float.IsInfinity(f))
                    {
                        error = $"{Name} must be a number like 0.5, got '{token}'";
                        return false;
                    }

                    if (!InRange(f, out error))
                    {
                        return false;
                    }

                    value = f;
                    return true;

                case ParameterKind.Enum:
                    foreach (string choice in _choices)
                    {
                        if (string.Equals(choice, token, StringComparison.OrdinalIgnoreCase))
                        {
                            value = System.Enum.Parse(EnumType, choice);
                            return true;
                        }
                    }

                    error = $"unknown {Name} '{token}' (use {string.Join(", ", _choices)})";
                    return false;

                case ParameterKind.Choice:
                    foreach (string choice in _choices)
                    {
                        if (string.Equals(choice, token, StringComparison.OrdinalIgnoreCase))
                        {
                            value = choice;
                            return true;
                        }
                    }

                    error = $"{Name} must be one of {string.Join(", ", _choices)}, got '{token}'";
                    return false;

                case ParameterKind.Text:
                case ParameterKind.Rest:
                    value = token;
                    return true;

                default:
                    error = $"{Name} can't be parsed here";
                    return false;
            }
        }

        private bool InRange(double number, out string error)
        {
            error = null;
            if ((Min.HasValue && number < Min.Value) || (Max.HasValue && number > Max.Value))
            {
                error = $"{Name} must be between {FormatValue(Min ?? double.MinValue)} and {FormatValue(Max ?? double.MaxValue)}";
                return false;
            }

            return true;
        }

        internal static string FormatValue(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case float f:
                    return f.ToString("0.###", CultureInfo.InvariantCulture);
                case double d:
                    return d.ToString("0.###", CultureInfo.InvariantCulture);
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }
    }

    /// <summary>Factory for <see cref="CommandParameter"/>, e.g. <c>P.Float("burn").Default(0f)</c>.</summary>
    public static class P
    {
        public static CommandParameter Target(string name = "target") => new(name, ParameterKind.Target);

        public static CommandParameter Int(string name) => new(name, ParameterKind.Int);

        public static CommandParameter Float(string name) => new(name, ParameterKind.Float);

        /// <summary>One of <typeparamref name="T"/>'s names. Pass <paramref name="allowed"/> to accept only some of them.</summary>
        public static CommandParameter Enum<T>(string name, params T[] allowed) where T : struct, System.Enum
        {
            IEnumerable<T> values = allowed != null && allowed.Length > 0
                ? allowed
                : System.Enum.GetValues(typeof(T)).Cast<T>();
            return new CommandParameter(name, ParameterKind.Enum).WithEnum(typeof(T), values.Select(v => v.ToString()).Distinct());
        }

        /// <summary>One of <paramref name="choices"/>, matched case-insensitively and returned in the spelling given here.</summary>
        public static CommandParameter Choice(string name, params string[] choices) =>
            new CommandParameter(name, ParameterKind.Choice).WithChoices(choices);

        public static CommandParameter Item(string name = "item") => new(name, ParameterKind.Item);

        public static CommandParameter Text(string name) => new(name, ParameterKind.Text);

        public static CommandParameter Rest(string name) => new(name, ParameterKind.Rest);
    }
}
