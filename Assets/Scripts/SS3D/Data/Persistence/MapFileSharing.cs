using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace SS3D.Data.Persistence
{
    /// <summary>
    /// Moves station templates in and out of the game folder so testers can share maps.
    /// Export zips one template into <c>Data/Exports/</c>, because large maps
    /// (MetaStation is ~12 MB of JSON) exceed Discord's upload limit unzipped. Import is
    /// "drop a <c>.zip</c> or <c>.json</c> into <c>Data/StationTemplates/</c>": zips are
    /// unpacked the next time templates are listed.
    /// </summary>
    public static class MapFileSharing
    {
        public const string ExportsFolderName = "Exports";
        private const string ImportedZipsFolderName = "ImportedZips";

        public static string StationTemplatesFolder =>
            Path.GetFullPath(Paths.GetPath(GamePaths.Data, true) + PersistencePaths.StationTemplates);

        public static string ExportsFolder =>
            Path.GetFullPath(Paths.GetPath(GamePaths.Data, true) + "/" + ExportsFolderName);

        /// <summary>
        /// Zips <c>StationTemplates/&lt;mapName&gt;.json</c> into <c>Exports/&lt;mapName&gt;.zip</c>,
        /// replacing an older export of the same name.
        /// </summary>
        public static bool TryExport(string mapName, out string zipPath, out string error) =>
            TryExport(StationTemplatesFolder, ExportsFolder, mapName, out zipPath, out error);

        public static bool TryExport(string templatesFolder, string exportsFolder, string mapName, out string zipPath, out string error)
        {
            zipPath = null;
            error = null;

            string source = Path.Combine(templatesFolder, mapName + ".json");
            if (!File.Exists(source))
            {
                error = $"{mapName} has not been saved yet.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(exportsFolder);
                zipPath = Path.Combine(exportsFolder, mapName + ".zip");
                string temp = zipPath + ".tmp";
                if (File.Exists(temp))
                    File.Delete(temp);

                using (FileStream stream = new(temp, FileMode.CreateNew))
                using (ZipArchive archive = new(stream, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry entry = archive.CreateEntry(mapName + ".json", CompressionLevel.Optimal);
                    using Stream entryStream = entry.Open();
                    using FileStream input = File.OpenRead(source);
                    input.CopyTo(entryStream);
                }

                if (File.Exists(zipPath))
                    File.Delete(zipPath);
                File.Move(temp, zipPath);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Unpacks every <c>.zip</c> dropped into the StationTemplates folder. Each <c>.json</c>
        /// entry becomes a template; a name that already exists gets a numbered suffix so nothing
        /// is overwritten. Processed zips move to <c>StationTemplates/ImportedZips/</c>.
        /// </summary>
        /// <returns>Names of the templates that were added.</returns>
        public static IReadOnlyList<string> ImportDroppedArchives() => ImportDroppedArchives(StationTemplatesFolder);

        public static IReadOnlyList<string> ImportDroppedArchives(string folder)
        {
            List<string> imported = new();
            if (!Directory.Exists(folder))
                return imported;

            foreach (string zip in Directory.GetFiles(folder, "*.zip"))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(zip))
                    using (ZipArchive archive = new(stream, ZipArchiveMode.Read))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (!entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                                continue;

                            // entry.Name drops any folder inside the zip, so paths cannot escape.
                            string name = UniqueTemplateName(folder, Path.GetFileNameWithoutExtension(entry.Name));
                            using (Stream input = entry.Open())
                            using (FileStream output = new(Path.Combine(folder, name + ".json"), FileMode.CreateNew))
                                input.CopyTo(output);
                            imported.Add(name);
                        }
                    }

                    string done = Path.Combine(folder, ImportedZipsFolderName);
                    Directory.CreateDirectory(done);
                    string target = Path.Combine(done, Path.GetFileName(zip));
                    if (File.Exists(target))
                        File.Delete(target);
                    File.Move(zip, target);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogWarning($"[MapFileSharing] Could not import {zip}: {exception.Message}");
                }
            }

            return imported;
        }

        /// <summary>
        /// Opens the OS file manager with <paramref name="path"/> selected (Windows, macOS) or its
        /// folder open (Linux). Falls back to opening the folder by URL.
        /// </summary>
        public static void Reveal(string path)
        {
            string folder = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            try
            {
                switch (UnityEngine.Application.platform)
                {
                    case UnityEngine.RuntimePlatform.WindowsPlayer:
                    case UnityEngine.RuntimePlatform.WindowsEditor:
                        Process.Start("explorer.exe", File.Exists(path) ? $"/select,\"{path}\"" : $"\"{folder}\"");
                        return;
                    case UnityEngine.RuntimePlatform.OSXPlayer:
                    case UnityEngine.RuntimePlatform.OSXEditor:
                        Process.Start("open", File.Exists(path) ? $"-R \"{path}\"" : $"\"{folder}\"");
                        return;
                    case UnityEngine.RuntimePlatform.LinuxPlayer:
                    case UnityEngine.RuntimePlatform.LinuxEditor:
                        Process.Start("xdg-open", $"\"{folder}\"");
                        return;
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"[MapFileSharing] Could not open file manager: {exception.Message}");
            }

            UnityEngine.Application.OpenURL(new Uri(folder).AbsoluteUri);
        }

        private static string UniqueTemplateName(string folder, string name)
        {
            if (!File.Exists(Path.Combine(folder, name + ".json")))
                return name;

            for (int i = 2; ; i++)
            {
                string candidate = $"{name} ({i})";
                if (!File.Exists(Path.Combine(folder, candidate + ".json")))
                    return candidate;
            }
        }
    }
}
