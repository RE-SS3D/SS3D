using SS3D.Systems.Examine;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SS3D.Systems.Examine.Editor
{
    internal static class ExamineLegacyEnglishResolver
    {
        public static string ResolveEnglishName(ExamineData data)
        {
            return ResolveEnglishText(data, data.NameKey, isDescription: false);
        }

        public static string ResolveEnglishDescription(ExamineData data)
        {
            return ResolveEnglishText(data, data.DescriptionKey, isDescription: true);
        }

        private static string ResolveEnglishText(ExamineData data, string legacyKey, bool isDescription)
        {
            if (string.IsNullOrEmpty(legacyKey))
            {
                return string.Empty;
            }

            if (ExamineCanonicalKeyGenerator.IsSnakeCaseIdentifier(legacyKey))
            {
                string tableValue = TryGetLegacyTableValue(data.LocalizationTable, legacyKey);
                if (!string.IsNullOrEmpty(tableValue))
                {
                    return tableValue;
                }

                if (isDescription && !legacyKey.EndsWith("_desc"))
                {
                    tableValue = TryGetLegacyTableValue(data.LocalizationTable, $"{legacyKey}_desc");
                    if (!string.IsNullOrEmpty(tableValue))
                    {
                        return tableValue;
                    }
                }
            }

            return legacyKey;
        }

        private static string TryGetLegacyTableValue(LocalizedStringTable tableReference, string key)
        {
            if (tableReference == null || string.IsNullOrEmpty(key))
            {
                return null;
            }

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableReference.TableReference);
            if (collection == null)
            {
                return null;
            }

            if (collection.GetTable(new LocaleIdentifier("en")) is not StringTable englishTable)
            {
                return null;
            }

            StringTableEntry entry = englishTable.GetEntry(key);
            return entry?.Value;
        }
    }
}
