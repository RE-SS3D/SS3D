using SS3D.Core;
using SS3D.Data.Management;
using SS3D.Data.Persistence;
using SS3D.Systems.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS3D.Systems.Tile.MapEditor.Persistence
{
    public sealed class MapEditorMapEntry
    {
        public string Name { get; init; }
        public string DisplayLabel { get; init; }
        public bool IsQuicksave { get; init; }
    }

    /// <summary>
    /// Abstracts map template persistence for the editor save/load UI.
    /// </summary>
    public interface IMapEditorPersistence
    {
        IReadOnlyList<MapEditorMapEntry> ListMaps();
        bool Save(string mapName, bool overwrite);
        bool Load(string mapName);
        void Delete(string mapName);
        bool Rename(string oldName, string newName);
        bool Exists(string mapName);
    }

    /// <summary>
    /// v1 persistence via existing TileSubSystem + LocalStorage.
    /// Passes bare template names into TileSubSystem — that API prepends
    /// <see cref="PersistencePaths.StationTemplates"/> itself (via PersistenceSubSystem).
    /// </summary>
    public sealed class MapEditorLocalPersistence : IMapEditorPersistence
    {
        private readonly TileSubSystem _tileSystem;
        private const string QuicksavePrefix = "quicksave_";

        /// <summary>
        /// Former bug wrote templates under StationTemplates/StationTemplates/ because the
        /// editor prepended SavePath and PersistenceSubSystem prepended it again.
        /// </summary>
        private static string DoubledStationTemplatesPath =>
            PersistencePaths.StationTemplates + PersistencePaths.StationTemplates;

        public MapEditorLocalPersistence(TileSubSystem tileSystem) => _tileSystem = tileSystem;

        public IReadOnlyList<MapEditorMapEntry> ListMaps()
        {
            MigrateDoubledSavePathIfNeeded();

            return EnumerateTemplateNames()
                .OrderByDescending(n => n.StartsWith(QuicksavePrefix, StringComparison.Ordinal))
                .ThenByDescending(n => n, StringComparer.OrdinalIgnoreCase)
                .Select(n => new MapEditorMapEntry
                {
                    Name = n,
                    DisplayLabel = n,
                    IsQuicksave = n.StartsWith(QuicksavePrefix, StringComparison.Ordinal),
                })
                .ToList();
        }

        public bool Save(string mapName, bool overwrite)
        {
            // Bare name only — TileSubSystem / PersistenceSubSystem add StationTemplates/.
            return SubSystems.TryGet(out PersistenceSubSystem persistence)
                && persistence.SaveStationTemplate(mapName, overwrite);
        }

        public bool Load(string mapName)
        {
            MigrateDoubledSavePathIfNeeded();
            _tileSystem.Load(mapName);
            return true;
        }

        public void Delete(string mapName)
        {
            if (LocalStorage.FolderAlreadyContainsName(PersistencePaths.StationTemplates, mapName))
                LocalStorage.DeleteFile(PersistencePaths.StationTemplates + "/" + mapName);

            if (LocalStorage.FolderAlreadyContainsName(PersistencePaths.LegacyTilemaps, mapName))
                LocalStorage.DeleteFile(PersistencePaths.LegacyTilemaps + "/" + mapName);

            if (LocalStorage.FolderAlreadyContainsName(DoubledStationTemplatesPath, mapName))
                LocalStorage.DeleteFile(DoubledStationTemplatesPath + "/" + mapName);
        }

        public bool Rename(string oldName, string newName)
        {
            if (Exists(newName))
                return false;

            MigrateDoubledSavePathIfNeeded();

            if (LocalStorage.FolderAlreadyContainsName(PersistencePaths.StationTemplates, oldName))
            {
                LocalStorage.RenameFile(
                    PersistencePaths.StationTemplates + "/" + oldName,
                    PersistencePaths.StationTemplates + "/" + newName);
                return true;
            }

            if (LocalStorage.FolderAlreadyContainsName(PersistencePaths.LegacyTilemaps, oldName))
            {
                LocalStorage.RenameFile(
                    PersistencePaths.LegacyTilemaps + "/" + oldName,
                    PersistencePaths.LegacyTilemaps + "/" + newName);
                return true;
            }

            return false;
        }

        public bool Exists(string mapName) => _tileSystem.MapNameAlreadyExist(mapName);

        private static IEnumerable<string> EnumerateTemplateNames()
        {
            if (SubSystems.TryGet(out PersistenceSubSystem persistence))
                return persistence.ListStationTemplates();

            return StripExtensions(LocalStorage.GetAllObjectsNameInFolder(PersistencePaths.StationTemplates))
                .Concat(StripExtensions(LocalStorage.GetAllObjectsNameInFolder(PersistencePaths.LegacyTilemaps)))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> StripExtensions(IEnumerable<string> fileNames)
        {
            foreach (string file in fileNames)
            {
                int dot = file.LastIndexOf('.');
                yield return dot > 0 ? file[..dot] : file;
            }
        }

        /// <summary>
        /// Moves files from the doubled StationTemplates/StationTemplates folder up one level
        /// when the correct slot is empty, so previously "invisible" saves show up in the list.
        /// </summary>
        private static void MigrateDoubledSavePathIfNeeded()
        {
            List<string> nestedFiles = LocalStorage.GetAllObjectsNameInFolder(DoubledStationTemplatesPath);
            if (nestedFiles.Count == 0)
                return;

            foreach (string file in nestedFiles)
            {
                int dot = file.LastIndexOf('.');
                string name = dot > 0 ? file[..dot] : file;
                if (LocalStorage.FolderAlreadyContainsName(PersistencePaths.StationTemplates, name))
                    continue;

                LocalStorage.RenameFile(
                    DoubledStationTemplatesPath + "/" + name,
                    PersistencePaths.StationTemplates + "/" + name);
            }
        }
    }
}
