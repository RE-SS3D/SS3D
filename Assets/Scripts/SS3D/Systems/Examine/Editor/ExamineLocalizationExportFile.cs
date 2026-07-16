using System;

namespace SS3D.Systems.Examine.Editor
{
    public class ExamineLocalizationExportFile
    {
        public int Version { get; set; } = 1;

        public string ExportedAt { get; set; }

        public ExamineLocalizationExportEntry[] Entries { get; set; } = Array.Empty<ExamineLocalizationExportEntry>();
    }
}
