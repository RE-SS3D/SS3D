using System;
using System.Collections.Generic;

namespace SS3D.Systems.Area
{
    /// <summary>The fixed department list from area.md §2. <c>AreaRecord.ParentTag</c> holds one of these or nothing.</summary>
    public static class AreaDepartments
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "Command", "Security", "Engineering", "Medical", "Science", "Service", "Cargo", "Maintenance", "Hallways",
        };

        /// <summary>Matches a tag case-insensitively and returns its listed spelling, or empty for anything else.</summary>
        public static string Normalize(string tag)
        {
            string trimmed = tag?.Trim();
            foreach (string department in All)
            {
                if (string.Equals(department, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return department;
                }
            }

            return string.Empty;
        }
    }
}
