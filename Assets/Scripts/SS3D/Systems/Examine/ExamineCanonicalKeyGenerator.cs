using System;
using System.Collections.Generic;
using System.Text;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Generates stable canonical localization keys for <see cref="ExamineData"/> assets.
    /// </summary>
    public static class ExamineCanonicalKeyGenerator
    {
        public const string ExamineDataRoot = "Assets/Content/Data/Examine/String";
        public const string ExamineTableName = "Examine";
        public const string NameSuffix = ".name";
        public const string DescSuffix = ".desc";
        public const string DefaultExportPath = "Documents/localization/examine_strings_export.json";

        private static readonly HashSet<string> SkippedPathFolders = new(StringComparer.Ordinal)
        {
            "Functional",
            "Generic",
        };

        public static string GetNameKey(string assetPath, string legacyNameKey)
        {
            if (IsSnakeCaseIdentifier(legacyNameKey))
            {
                return $"{GetFolderPathKey(assetPath)}.{legacyNameKey}{NameSuffix}";
            }

            return $"{GetAssetPathKey(assetPath)}{NameSuffix}";
        }

        public static string GetDescriptionKey(string assetPath)
        {
            return $"{GetAssetPathKey(assetPath)}{DescSuffix}";
        }

        public static string GetAssetPathKey(string assetPath)
        {
            return string.Join(".", GetPathSegments(assetPath, includeFileName: true));
        }

        public static string GetFolderPathKey(string assetPath)
        {
            IReadOnlyList<string> segments = GetPathSegments(assetPath, includeFileName: false);
            return segments.Count == 0 ? string.Empty : string.Join(".", segments);
        }

        public static bool IsSnakeCaseIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsUpper(character) || char.IsWhiteSpace(character))
                {
                    return false;
                }

                if (i == 0 && !char.IsLower(character) && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        public static string ToSnakeCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder builder = new(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsUpper(character) && i > 0)
                {
                    char previous = value[i - 1];
                    bool nextIsLower = i + 1 < value.Length && char.IsLower(value[i + 1]);
                    if (!char.IsUpper(previous) || nextIsLower)
                    {
                        builder.Append('_');
                    }
                }
                else if (char.IsDigit(character) && i > 0 && char.IsLower(value[i - 1]))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(character));
            }

            return builder.ToString();
        }

        private static IReadOnlyList<string> GetPathSegments(string assetPath, bool includeFileName)
        {
            string relativePath = GetRelativeExaminePath(assetPath);
            if (string.IsNullOrEmpty(relativePath))
            {
                return Array.Empty<string>();
            }

            string normalized = relativePath.Replace('\\', '/');
            int extensionIndex = normalized.LastIndexOf(".asset", StringComparison.OrdinalIgnoreCase);
            if (extensionIndex >= 0)
            {
                normalized = normalized[..extensionIndex];
            }

            string[] rawSegments = normalized.Split('/');
            List<string> segments = new(rawSegments.Length);
            for (int i = 0; i < rawSegments.Length; i++)
            {
                bool isFileName = i == rawSegments.Length - 1;
                if (!includeFileName && isFileName)
                {
                    continue;
                }

                if (!isFileName && SkippedPathFolders.Contains(rawSegments[i]))
                {
                    continue;
                }

                segments.Add(ToSnakeCase(rawSegments[i]));
            }

            return segments;
        }

        private static string GetRelativeExaminePath(string assetPath)
        {
            string normalized = assetPath.Replace('\\', '/');
            if (!normalized.StartsWith(ExamineDataRoot, StringComparison.Ordinal))
            {
                return null;
            }

            string relative = normalized[ExamineDataRoot.Length..].TrimStart('/');
            return string.IsNullOrEmpty(relative) ? null : relative;
        }
    }
}
