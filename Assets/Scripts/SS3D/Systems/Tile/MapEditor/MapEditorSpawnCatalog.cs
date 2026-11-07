using System;
using SS3D.Systems.Tile.SpawnPoints;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Stable catalog keys for spawn-point library entries (<c>spawn:job:…</c> / <c>spawn:antag:…</c>).
    /// </summary>
    public static class MapEditorSpawnCatalog
    {
        public const string JobPrefix = "spawn:job:";
        public const string AntagPrefix = "spawn:antag:";

        /// <summary>
        /// Job tags available for authoring. Mirrors current <c>RoleData</c> names
        /// (Assistant / Security); extend when new roles ship.
        /// </summary>
        public static readonly string[] DefaultJobNames = { "Assistant", "Security" };

        public static string EncodeJob(string jobName) => $"{JobPrefix}{jobName}";

        public static string EncodeAntagonist(AntagonistSpawnCategory category) =>
            $"{AntagPrefix}{category}";

        public static bool TryDecode(string assetName, out SpawnPointKind kind, out string jobName,
            out AntagonistSpawnCategory category)
        {
            kind = default;
            jobName = null;
            category = default;

            if (string.IsNullOrEmpty(assetName))
                return false;

            if (assetName.StartsWith(JobPrefix, StringComparison.OrdinalIgnoreCase))
            {
                jobName = assetName.Substring(JobPrefix.Length);
                if (string.IsNullOrWhiteSpace(jobName))
                    return false;

                kind = SpawnPointKind.Job;
                return true;
            }

            if (assetName.StartsWith(AntagPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string token = assetName.Substring(AntagPrefix.Length);
                if (!Enum.TryParse(token, ignoreCase: true, out category))
                    return false;

                kind = SpawnPointKind.Antagonist;
                return true;
            }

            return false;
        }

        public static bool IsSpawnKey(string assetName) =>
            TryDecode(assetName, out _, out _, out _);

        public static string DisplayName(string assetName)
        {
            if (!TryDecode(assetName, out SpawnPointKind kind, out string jobName,
                    out AntagonistSpawnCategory category))
                return assetName;

            return kind == SpawnPointKind.Job
                ? $"Job: {jobName}"
                : $"Antag: {FormatAntagonist(category)}";
        }

        /// <summary>
        /// Antagonists that enter the round at a marker, and so get a palette entry. Traitor and Malfunctioning AI are
        /// assigned to a crew member or the AI already aboard, so a marker for them would do nothing.
        /// </summary>
        public static readonly AntagonistSpawnCategory[] MarkerAntagonists = { AntagonistSpawnCategory.NuclearOperatives };

        public static string FormatAntagonist(AntagonistSpawnCategory category) =>
            category switch
            {
                AntagonistSpawnCategory.Traitor => "Traitor",
                AntagonistSpawnCategory.MalfunctioningAI => "Malfunctioning AI",
                AntagonistSpawnCategory.NuclearOperatives => "Nuclear Operatives",
                _ => category.ToString(),
            };

        public static SpawnPointRecord ToRecord(string assetName, Vector3 position, Direction direction)
        {
            if (!TryDecode(assetName, out SpawnPointKind kind, out string jobName,
                    out AntagonistSpawnCategory category))
                return default;

            return kind == SpawnPointKind.Job
                ? SpawnPointRecord.ForJob(jobName, position, direction)
                : SpawnPointRecord.ForAntagonist(category, position, direction);
        }
    }
}
