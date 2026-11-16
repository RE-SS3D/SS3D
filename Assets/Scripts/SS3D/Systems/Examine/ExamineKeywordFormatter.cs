using System.Collections.Generic;
using System.Text;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Turns examine keyword markup into UI Toolkit rich text (examine.md §3). Localized strings wrap a
    /// word in <c>&lt;k=good&gt;</c>, <c>&lt;k=warn&gt;</c>, <c>&lt;k=danger&gt;</c> or
    /// <c>&lt;k=dept:&lt;id&gt;&gt;</c> and close it with <c>&lt;/k&gt;</c>. A known tag becomes a
    /// <c>&lt;color&gt;</c> span; an unknown or malformed one is dropped and the word stays plain, so the
    /// word always carries the meaning on its own. Other rich-text tags pass through untouched.
    /// </summary>
    public static class ExamineKeywordFormatter
    {
        public const string Good = "good";
        public const string Warn = "warn";
        public const string Danger = "danger";
        public const string DepartmentPrefix = "dept:";

        private const string OpenPrefix = "<k=";
        private const string Close = "</k>";

        // Mirrors --ss3d-feedback-success|warning|danger in ss3d-tokens.uss. Rich text cannot read USS
        // variables; ExamineKeywordFormatterTests fails if these drift from the token file.
        public const string GoodColor = "#4a8f5c";
        public const string WarnColor = "#c9a227";
        public const string DangerColor = "#b94848";

        /// <summary>
        /// Department colours by department id (lower case). Empty until the examine-feedback visual
        /// brief settles the palette; until then a department tag renders as plain text.
        /// </summary>
        private static readonly Dictionary<string, string> DepartmentColors = new();

        /// <summary>True if <paramref name="tag"/> is a tag the formatter knows (dept tags with any id).</summary>
        public static bool IsKnownTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return false;
            }

            return tag == Good
                || tag == Warn
                || tag == Danger
                || (tag.StartsWith(DepartmentPrefix) && tag.Length > DepartmentPrefix.Length);
        }

        /// <summary>Every tag name in <paramref name="text"/>, in order, for audits.</summary>
        public static IEnumerable<string> EnumerateTags(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            int index = 0;
            while ((index = text.IndexOf(OpenPrefix, index, System.StringComparison.Ordinal)) >= 0)
            {
                int end = text.IndexOf('>', index + OpenPrefix.Length);
                if (end < 0)
                {
                    yield break;
                }

                yield return text.Substring(index + OpenPrefix.Length, end - index - OpenPrefix.Length);
                index = end + 1;
            }
        }

        /// <summary>Markup → rich text with colour spans.</summary>
        public static string Format(string text) => Rewrite(text, colorize: true);

        /// <summary>Markup → plain text with keyword tags removed (for logs and tests).</summary>
        public static string StripTags(string text) => Rewrite(text, colorize: false);

        private static string Rewrite(string text, bool colorize)
        {
            if (string.IsNullOrEmpty(text)
                || (text.IndexOf(OpenPrefix, System.StringComparison.Ordinal) < 0
                    && text.IndexOf(Close, System.StringComparison.Ordinal) < 0))
            {
                return text ?? string.Empty;
            }

            StringBuilder builder = new(text.Length + 32);
            // One entry per open tag: whether it emitted a <color> span that its close must end.
            Stack<bool> open = new();
            int i = 0;
            while (i < text.Length)
            {
                if (string.CompareOrdinal(text, i, OpenPrefix, 0, OpenPrefix.Length) == 0)
                {
                    int end = text.IndexOf('>', i + OpenPrefix.Length);
                    if (end > 0)
                    {
                        string tag = text.Substring(i + OpenPrefix.Length, end - i - OpenPrefix.Length);
                        string color = colorize ? ResolveColor(tag) : null;
                        if (color != null)
                        {
                            builder.Append("<color=").Append(color).Append('>');
                        }

                        open.Push(color != null);
                        i = end + 1;
                        continue;
                    }
                }

                if (string.CompareOrdinal(text, i, Close, 0, Close.Length) == 0)
                {
                    if (open.Count > 0 && open.Pop())
                    {
                        builder.Append("</color>");
                    }

                    i += Close.Length;
                    continue;
                }

                builder.Append(text[i]);
                i++;
            }

            while (open.Count > 0)
            {
                if (open.Pop())
                {
                    builder.Append("</color>");
                }
            }

            return builder.ToString();
        }

        private static string ResolveColor(string tag)
        {
            switch (tag)
            {
                case Good:
                    return GoodColor;
                case Warn:
                    return WarnColor;
                case Danger:
                    return DangerColor;
            }

            if (tag != null && tag.StartsWith(DepartmentPrefix)
                && DepartmentColors.TryGetValue(tag.Substring(DepartmentPrefix.Length).ToLowerInvariant(), out string color))
            {
                return color;
            }

            return null;
        }
    }
}
