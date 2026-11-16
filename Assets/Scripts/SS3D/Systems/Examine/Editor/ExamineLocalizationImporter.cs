using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SS3D.Systems.Examine.Editor
{
    public static class ExamineLocalizationImporter
    {
        public struct ImportResult
        {
            public int EntriesWritten;
            public int AssetsUpdated;
            public int DuplicateKeysSkipped;
        }

        public static ImportResult ImportEnglishMigration(ExamineLocalizationExportFile exportFile, bool updateExamineDataAssets)
        {
            ImportResult result = default;
            if (exportFile?.Entries == null || exportFile.Entries.Length == 0)
            {
                return result;
            }

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(ExamineCanonicalKeyGenerator.ExamineTableName);
            if (collection == null)
            {
                throw new FileNotFoundException($"Could not find string table collection '{ExamineCanonicalKeyGenerator.ExamineTableName}'.");
            }

            if (collection.GetTable(new LocaleIdentifier("en")) is not StringTable englishTable)
            {
                throw new FileNotFoundException("Could not find English table for the Examine collection.");
            }

            Dictionary<string, string> localizedValues = new();
            foreach (ExamineLocalizationExportEntry entry in exportFile.Entries)
            {
                TrackLocalizedValue(localizedValues, entry.NameKey, entry.EnName, ref result);
                TrackLocalizedValue(localizedValues, entry.DescriptionKey, entry.EnDescription, ref result);
            }

            foreach (KeyValuePair<string, string> localizedValue in localizedValues)
            {
                UpsertEntry(englishTable, localizedValue.Key, localizedValue.Value);
                result.EntriesWritten++;
            }

            EditorUtility.SetDirty(englishTable);
            EditorUtility.SetDirty(englishTable.SharedData);
            EditorUtility.SetDirty(collection);

            if (updateExamineDataAssets)
            {
                foreach (ExamineLocalizationExportEntry entry in exportFile.Entries)
                {
                    ExamineData data = AssetDatabase.LoadAssetAtPath<ExamineData>(entry.AssetPath);
                    if (data == null)
                    {
                        continue;
                    }

                    data.Name.SetReference(ExamineCanonicalKeyGenerator.ExamineTableName, entry.NameKey);
                    data.Description.SetReference(ExamineCanonicalKeyGenerator.ExamineTableName, entry.DescriptionKey);
                    EditorUtility.SetDirty(data);
                    result.AssetsUpdated++;
                }
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        public static ImportResult ImportEnglishMigrationFromFile(string inputPath, bool updateExamineDataAssets)
        {
            string json = File.ReadAllText(inputPath);
            ExamineLocalizationExportFile exportFile = ExamineLocalizationJson.Deserialize(json);
            return ImportEnglishMigration(exportFile, updateExamineDataAssets);
        }

        private static void TrackLocalizedValue(
            Dictionary<string, string> localizedValues,
            string key,
            string value,
            ref ImportResult result)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (localizedValues.TryGetValue(key, out string existingValue))
            {
                if (existingValue != value)
                {
                    Debug.LogWarning($"Examine localization import kept first value for duplicate key '{key}'.");
                }

                result.DuplicateKeysSkipped++;
                return;
            }

            localizedValues[key] = value ?? string.Empty;
        }

        private static void UpsertEntry(StringTable table, string key, string value)
        {
            StringTableEntry entry = table.GetEntry(key);
            if (entry == null)
            {
                table.AddEntry(key, value);
                return;
            }

            entry.Value = value;
        }
    }
}
