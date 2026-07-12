using System;
using UnityEngine;

namespace SS3D.Systems.Examine.Editor
{
    internal static class ExamineLocalizationJson
    {
#pragma warning disable SA1401 // JsonUtility requires public fields on serializable types.

        [Serializable]
        private class ExportFileDto
        {
            public int Version;
            public string ExportedAt;
            public ExportEntryDto[] Entries;
        }

        [Serializable]
        private class ExportEntryDto
        {
            public string AssetPath;
            public string NameKey;
            public string DescriptionKey;
            public string EnName;
            public string EnDescription;
        }

#pragma warning restore SA1401

        public static string Serialize(ExamineLocalizationExportFile exportFile)
        {
            return JsonUtility.ToJson(ToDto(exportFile), prettyPrint: true);
        }

        public static ExamineLocalizationExportFile Deserialize(string json)
        {
            ExportFileDto dto = JsonUtility.FromJson<ExportFileDto>(json);
            return FromDto(dto);
        }

        private static ExportFileDto ToDto(ExamineLocalizationExportFile exportFile)
        {
            ExportEntryDto[] entryDtos = new ExportEntryDto[exportFile.Entries.Length];
            for (int i = 0; i < exportFile.Entries.Length; i++)
            {
                entryDtos[i] = ToDto(exportFile.Entries[i]);
            }

            return new ExportFileDto
            {
                Version = exportFile.Version,
                ExportedAt = exportFile.ExportedAt,
                Entries = entryDtos,
            };
        }

        private static ExportEntryDto ToDto(ExamineLocalizationExportEntry entry)
        {
            return new ExportEntryDto
            {
                AssetPath = entry.AssetPath,
                NameKey = entry.NameKey,
                DescriptionKey = entry.DescriptionKey,
                EnName = entry.EnName,
                EnDescription = entry.EnDescription,
            };
        }

        private static ExamineLocalizationExportFile FromDto(ExportFileDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            ExamineLocalizationExportEntry[] entries = Array.Empty<ExamineLocalizationExportEntry>();
            if (dto.Entries != null)
            {
                entries = new ExamineLocalizationExportEntry[dto.Entries.Length];
                for (int i = 0; i < dto.Entries.Length; i++)
                {
                    entries[i] = FromDto(dto.Entries[i]);
                }
            }

            return new ExamineLocalizationExportFile
            {
                Version = dto.Version,
                ExportedAt = dto.ExportedAt,
                Entries = entries,
            };
        }

        private static ExamineLocalizationExportEntry FromDto(ExportEntryDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new ExamineLocalizationExportEntry
            {
                AssetPath = dto.AssetPath,
                NameKey = dto.NameKey,
                DescriptionKey = dto.DescriptionKey,
                EnName = dto.EnName,
                EnDescription = dto.EnDescription,
            };
        }
    }
}
