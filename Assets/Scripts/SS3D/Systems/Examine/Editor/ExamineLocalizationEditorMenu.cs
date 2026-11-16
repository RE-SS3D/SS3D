using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Examine.Editor
{
    public static class ExamineLocalizationEditorMenu
    {
        private const string MenuRoot = "SS3D/Localization/Examine/";

        [MenuItem(MenuRoot + "Export Strings to JSON")]
        public static void ExportStringsToJson()
        {
            string outputPath = EditorUtility.SaveFilePanel(
                "Export Examine Strings",
                "Documents/localization",
                "examine_strings_export.json",
                "json");

            if (string.IsNullOrEmpty(outputPath))
            {
                return;
            }

            ExamineLocalizationExporter.ExportToFile(outputPath);
            Debug.Log($"Exported examine strings to '{outputPath}'.");
        }

        [MenuItem(MenuRoot + "Import English Migration from JSON")]
        public static void ImportEnglishMigrationFromJson()
        {
            string inputPath = EditorUtility.OpenFilePanel(
                "Import Examine English Migration",
                "Documents/localization",
                "json");

            if (string.IsNullOrEmpty(inputPath))
            {
                return;
            }

            ImportEnglishMigration(inputPath);
        }

        [MenuItem(MenuRoot + "Export and Import English Migration")]
        public static void ExportAndImportEnglishMigration()
        {
            RunEnglishMigration(logExportPath: true);
        }

        /// <summary>
        /// Batch-mode entry point for CI or headless migration runs.
        /// </summary>
        public static void RunEnglishMigrationBatch()
        {
            RunEnglishMigration(logExportPath: true);
            EditorApplication.Exit(0);
        }

        private static void RunEnglishMigration(bool logExportPath)
        {
            ExamineLocalizationExportFile exportFile = ExamineLocalizationExporter.ExportAll();
            ExamineLocalizationImporter.ImportResult result = ExamineLocalizationImporter.ImportEnglishMigration(
                exportFile,
                updateExamineDataAssets: true);

            const string exportPath = ExamineCanonicalKeyGenerator.DefaultExportPath;
            ExamineLocalizationExporter.ExportToFile(exportFile, exportPath);

            if (logExportPath)
            {
                Debug.Log(
                    $"Examine English migration complete. Entries written: {result.EntriesWritten}, " +
                    $"assets updated: {result.AssetsUpdated}, duplicate keys skipped: {result.DuplicateKeysSkipped}. " +
                    $"Export saved to '{exportPath}'.");
            }
        }

        private static void ImportEnglishMigration(string inputPath)
        {
            ExamineLocalizationImporter.ImportResult result = ExamineLocalizationImporter.ImportEnglishMigrationFromFile(
                inputPath,
                updateExamineDataAssets: true);

            Debug.Log(
                $"Examine English migration imported from '{inputPath}'. Entries written: {result.EntriesWritten}, " +
                $"assets updated: {result.AssetsUpdated}, duplicate keys skipped: {result.DuplicateKeysSkipped}.");
        }
    }
}
