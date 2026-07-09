using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace SS3D.Systems.Examine.Editor
{
    public static class ExamineLocalizationExporter
    {
        public static ExamineLocalizationExportFile ExportAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:ExamineData", new string[] { ExamineCanonicalKeyGenerator.ExamineDataRoot });
            System.Collections.Generic.List<ExamineLocalizationExportEntry> entries = new(guids.Length);

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ExamineData data = AssetDatabase.LoadAssetAtPath<ExamineData>(assetPath);
                if (data == null)
                {
                    continue;
                }

                entries.Add(CreateEntry(assetPath, data));
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.AssetPath, right.AssetPath));

            return new ExamineLocalizationExportFile
            {
                Version = 1,
                ExportedAt = System.DateTime.UtcNow.ToString("o"),
                Entries = entries.ToArray(),
            };
        }

        public static void ExportToFile(string outputPath)
        {
            ExportToFile(ExportAll(), outputPath);
        }

        public static void ExportToFile(ExamineLocalizationExportFile exportFile, string outputPath)
        {
            string json = ExamineLocalizationJson.Serialize(exportFile);
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outputPath, json);
            AssetDatabase.Refresh();
        }

        private static ExamineLocalizationExportEntry CreateEntry(string assetPath, ExamineData data)
        {
            return new ExamineLocalizationExportEntry
            {
                AssetPath = assetPath,
                NameKey = data.Name.TableEntryReference.Key,
                DescriptionKey = data.Description.TableEntryReference.Key,
                EnName = GetEnglishValue(data.Name),
                EnDescription = GetEnglishValue(data.Description),
            };
        }

        private static string GetEnglishValue(LocalizedString localizedString)
        {
            if (localizedString.IsEmpty)
            {
                return string.Empty;
            }

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(localizedString.TableReference);
            if (collection == null)
            {
                return string.Empty;
            }

            if (collection.GetTable(new LocaleIdentifier("en")) is not StringTable englishTable)
            {
                return string.Empty;
            }

            StringTableEntry entry = englishTable.GetEntry(localizedString.TableEntryReference.Key);
            return entry?.Value ?? string.Empty;
        }
    }
}
