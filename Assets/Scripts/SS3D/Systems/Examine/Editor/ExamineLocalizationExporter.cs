using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace SS3D.Systems.Examine.Editor
{
    public static class ExamineLocalizationExporter
    {
        public static ExamineLocalizationExportFile ExportAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:ExamineData", new string[] { ExamineCanonicalKeyGenerator.ExamineDataRoot });
            List<ExamineLocalizationExportEntry> entries = new(guids.Length);

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
                ExportedAt = DateTime.UtcNow.ToString("o"),
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
                NameKey = ExamineCanonicalKeyGenerator.GetNameKey(assetPath, data.NameKey),
                DescriptionKey = ExamineCanonicalKeyGenerator.GetDescriptionKey(assetPath),
                EnName = ExamineLegacyEnglishResolver.ResolveEnglishName(data),
                EnDescription = ExamineLegacyEnglishResolver.ResolveEnglishDescription(data),
            };
        }
    }
}
