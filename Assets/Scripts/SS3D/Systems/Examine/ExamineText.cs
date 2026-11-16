using System;
using SS3D.Localization;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Examine-table lookup that falls back to the English template, formatted with the same
    /// arguments, when the key is missing or the table has not loaded (EditMode, early frames).
    /// </summary>
    public static class ExamineText
    {
        private const string MissingPrefix = "[MISSING:";

        public static string Get(string key, string englishTemplate, params object[] arguments)
        {
            object[] args = arguments != null && arguments.Length > 0 ? arguments : null;
            string fallback = args == null || string.IsNullOrEmpty(englishTemplate)
                ? englishTemplate
                : string.Format(englishTemplate, args);

            string result = LocalizedTextService.GetFormattedString(
                ExamineCanonicalKeyGenerator.ExamineTableName, key, args, fallback);

            if (!string.IsNullOrEmpty(fallback)
                && (string.IsNullOrEmpty(result) || result.StartsWith(MissingPrefix, StringComparison.Ordinal)))
            {
                return fallback;
            }

            return result;
        }
    }
}
