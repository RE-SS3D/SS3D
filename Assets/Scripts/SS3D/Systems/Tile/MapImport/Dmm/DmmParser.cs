using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SS3D.Systems.Tile.MapImport.Dmm
{
    /// <summary>
    /// Parses classic and TGM-style SS13 .dmm text into cells. Z-levels other than the
    /// first encountered block are included; callers may filter by Z or bbox.
    /// </summary>
    public static class DmmParser
    {
        private static readonly Regex PrefabHeader =
            new Regex(@"^""([^""]+)""\s*=\s*\(", RegexOptions.Compiled);

        private static readonly Regex GridHeader =
            new Regex(@"^\((\d+)\s*,\s*(\d+)\s*,\s*(\d+)\)\s*=\s*\{""\s*$", RegexOptions.Compiled);

        public static DmmMap ParseFile(string path) => Parse(File.ReadAllText(path));

        public static DmmMap Parse(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            DmmMap map = new DmmMap();
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int i = 0;
            while (i < lines.Length)
            {
                string line = lines[i].TrimEnd();
                Match prefab = PrefabHeader.Match(line.TrimStart());
                if (prefab.Success)
                {
                    string key = prefab.Groups[1].Value;
                    int openIdx = line.IndexOf('(', StringComparison.Ordinal);
                    string afterOpen = openIdx >= 0 ? line[(openIdx + 1)..] : string.Empty;
                    i = ParsePrefabBody(lines, i, afterOpen, out List<DmmAtom> atoms);
                    map.Prefabs[key] = atoms;
                    if (key.Length > map.KeyLength)
                        map.KeyLength = key.Length;
                    continue;
                }

                Match grid = GridHeader.Match(line.Trim());
                if (grid.Success)
                {
                    int ox = int.Parse(grid.Groups[1].Value, CultureInfo.InvariantCulture);
                    int oy = int.Parse(grid.Groups[2].Value, CultureInfo.InvariantCulture);
                    int oz = int.Parse(grid.Groups[3].Value, CultureInfo.InvariantCulture);
                    i++;
                    List<string> rows = new List<string>();
                    while (i < lines.Length)
                    {
                        string rowLine = lines[i];
                        if (rowLine.Trim() == "\"}")
                        {
                            i++;
                            break;
                        }

                        rows.Add(rowLine.TrimEnd('\r'));
                        i++;
                    }

                    ExpandGrid(map, ox, oy, oz, rows);
                    continue;
                }

                i++;
            }

            if (map.KeyLength <= 0)
                map.KeyLength = 1;

            return map;
        }

        private static int ParsePrefabBody(string[] lines, int startLine, string firstAfterOpen, out List<DmmAtom> atoms)
        {
            atoms = new List<DmmAtom>();
            StringBuilder body = new StringBuilder();
            if (!string.IsNullOrEmpty(firstAfterOpen))
                body.AppendLine(firstAfterOpen);

            int i = startLine;
            int depth = CountParens(firstAfterOpen);
            // PrefabHeader already consumed the opening '(' on the header line.
            depth += 1;
            if (depth <= 0)
            {
                ParseAtomList(StripTrailingClose(firstAfterOpen), atoms);
                return startLine + 1;
            }

            i++;
            while (i < lines.Length && depth > 0)
            {
                string line = lines[i];
                depth += CountParens(line);
                body.AppendLine(line);
                i++;
            }

            string raw = body.ToString();
            raw = StripTrailingClose(raw);
            ParseAtomList(raw, atoms);
            return i;
        }

        private static string StripTrailingClose(string raw)
        {
            int close = raw.LastIndexOf(')');
            if (close >= 0)
                raw = raw[..close];
            return raw.Trim();
        }

        private static int CountParens(string s)
        {
            int d = 0;
            bool inString = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"' && (i == 0 || s[i - 1] != '\\'))
                    inString = !inString;
                if (inString)
                    continue;
                if (c == '(')
                    d++;
                else if (c == ')')
                    d--;
            }

            return d;
        }

        private static void ParseAtomList(string body, List<DmmAtom> into)
        {
            List<string> parts = SplitTopLevelCommas(body);
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (trimmed.Length == 0)
                    continue;
                into.Add(ParseAtom(trimmed));
            }
        }

        private static DmmAtom ParseAtom(string text)
        {
            DmmAtom atom = new DmmAtom();
            int brace = text.IndexOf('{');
            if (brace < 0)
            {
                atom.Path = text.Trim().TrimEnd(',');
                return atom;
            }

            atom.Path = text[..brace].Trim();
            int end = text.LastIndexOf('}');
            string varsBlock = end > brace ? text[(brace + 1)..end] : text[(brace + 1)..];
            foreach (string assignment in SplitTopLevelCommas(varsBlock))
            {
                string a = assignment.Trim().TrimEnd(';');
                if (a.Length == 0)
                    continue;
                int eq = a.IndexOf('=');
                if (eq <= 0)
                    continue;
                string key = a[..eq].Trim();
                string value = a[(eq + 1)..].Trim().Trim('"');
                atom.Vars[key] = value;
            }

            return atom;
        }

        private static List<string> SplitTopLevelCommas(string text)
        {
            List<string> parts = new List<string>();
            StringBuilder current = new StringBuilder();
            int depthParen = 0;
            int depthBrace = 0;
            bool inString = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"' && (i == 0 || text[i - 1] != '\\'))
                    inString = !inString;

                if (!inString)
                {
                    if (c == '(')
                        depthParen++;
                    else if (c == ')')
                        depthParen--;
                    else if (c == '{')
                        depthBrace++;
                    else if (c == '}')
                        depthBrace--;
                    else if (c == ',' && depthParen == 0 && depthBrace == 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                        continue;
                    }
                }

                current.Append(c);
            }

            if (current.Length > 0)
                parts.Add(current.ToString());
            return parts;
        }

        private static void ExpandGrid(DmmMap map, int ox, int oy, int oz, List<string> rows)
        {
            if (rows.Count == 0 || map.KeyLength <= 0)
                return;

            int keyLen = map.KeyLength;
            // First row in the block is highest Y (BYOND / TGM convention).
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                string row = rows[rowIndex];
                int y = oy + (rows.Count - 1 - rowIndex);
                int col = 0;
                for (int xOffset = 0; xOffset + keyLen <= row.Length; xOffset += keyLen)
                {
                    string key = row.Substring(xOffset, keyLen);
                    DmmCell cell = new DmmCell
                    {
                        X = ox + col,
                        Y = y,
                        Z = oz,
                        Key = key,
                    };
                    if (map.Prefabs.TryGetValue(key, out List<DmmAtom> atoms))
                        cell.Atoms.AddRange(atoms);
                    map.Cells.Add(cell);
                    col++;
                }
            }
        }
    }
}
