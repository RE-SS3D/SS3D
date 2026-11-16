using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SS3D.Systems.Tile.MapImport
{
    /// <summary>
    /// Minimal YAML reader for ss13_type_map.yaml (defaults + prefixes list only).
    /// </summary>
    public static class Ss13TypeMapYaml
    {
        private static readonly Regex DefaultLine =
            new Regex(@"^\s*(floor|wall|window|door)\s*:\s*(\S+)\s*$", RegexOptions.Compiled);

        private static readonly Regex MatchLine =
            new Regex(@"^\s*-\s*match:\s*(\S+)\s*$", RegexOptions.Compiled);

        private static readonly Regex KindLine =
            new Regex(@"^\s*kind:\s*(\S+)\s*$", RegexOptions.Compiled);

        private static readonly Regex SoLine =
            new Regex(@"^\s*so:\s*(\S+)\s*$", RegexOptions.Compiled);

        public static Ss13TypeMapConfig ParseFile(string path) => Parse(File.ReadAllText(path));

        public static Ss13TypeMapConfig Parse(string text)
        {
            Ss13TypeMapConfig config = new Ss13TypeMapConfig();
            if (string.IsNullOrEmpty(text))
                return config;

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            Ss13TypeMapEntry current = null;
            bool inPrefixes = false;

            foreach (string raw in lines)
            {
                string line = raw;
                int comment = line.IndexOf('#');
                if (comment >= 0)
                    line = line[..comment];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string trimmed = line.TrimEnd();
                if (trimmed.Trim() == "defaults:")
                {
                    inPrefixes = false;
                    Flush(ref current, config);
                    continue;
                }

                if (trimmed.Trim() == "prefixes:")
                {
                    inPrefixes = true;
                    Flush(ref current, config);
                    continue;
                }

                Match def = DefaultLine.Match(trimmed);
                if (!inPrefixes && def.Success)
                {
                    string key = def.Groups[1].Value;
                    string value = def.Groups[2].Value;
                    switch (key)
                    {
                        case "floor": config.DefaultFloor = value; break;
                        case "wall": config.DefaultWall = value; break;
                        case "window": config.DefaultWindow = value; break;
                        case "door": config.DefaultDoor = value; break;
                    }

                    continue;
                }

                Match match = MatchLine.Match(trimmed);
                if (match.Success)
                {
                    Flush(ref current, config);
                    current = new Ss13TypeMapEntry { Match = match.Groups[1].Value };
                    inPrefixes = true;
                    continue;
                }

                if (current == null)
                    continue;

                Match kind = KindLine.Match(trimmed);
                if (kind.Success)
                {
                    current.Kind = ParseKind(kind.Groups[1].Value);
                    continue;
                }

                Match so = SoLine.Match(trimmed);
                if (so.Success)
                    current.So = so.Groups[1].Value;
            }

            Flush(ref current, config);
            config.Prefixes.Sort((a, b) => b.Match.Length.CompareTo(a.Match.Length));
            return config;
        }

        private static void Flush(ref Ss13TypeMapEntry current, Ss13TypeMapConfig config)
        {
            if (current == null)
                return;
            if (!string.IsNullOrEmpty(current.Match) && current.Kind != MapImportKind.Skip)
                config.Prefixes.Add(current);
            current = null;
        }

        private static MapImportKind ParseKind(string raw)
        {
            if (Enum.TryParse(raw, true, out MapImportKind kind))
                return kind;
            return MapImportKind.Skip;
        }
    }
}
