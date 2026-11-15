using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// One problem a domain's analyzer found (Documents/architecture/2026-11_map-editor-systems-views.md Q5, Phase 4). Map
    /// Check reports it as a warning (<see cref="Id"/>, <see cref="Title"/>, <see cref="Body"/>, Locate at
    /// <see cref="Position"/>), and the view puts a marker at each of <see cref="Markers"/> carrying the same id, so the
    /// two show the same problems by construction.
    /// </summary>
    public struct SystemsViewIssue
    {
        /// <summary>Map Check's <c>MapValidationIssue.Id</c>, and every marker's <see cref="SystemsViewMarker.IssueId"/>.</summary>
        public string Id;
        /// <summary>Map Check's category heading: "Atmos" or "Power".</summary>
        public string Category;
        public string Title;
        public string Body;
        /// <summary>A palette entry that fixes it (Map Check's Show in palette); null for none.</summary>
        public string EntryKey;
        /// <summary>Where Locate centres the camera: the first marked spot.</summary>
        public Vector3 Position;
        /// <summary>Where the view draws a marker: a tile plus <see cref="SystemsViewProblems.MarkerSlot"/>.</summary>
        public SystemsViewKey[] Markers;
    }

    /// <summary>
    /// A kind of problem in plain words, shared by the legend, the hover readout and Map Check's text (creative-mode §4
    /// "Plain words"): <see cref="Name"/> ("Open pipe end") and <see cref="Words"/>, what that means for the round.
    /// <see cref="Code"/> goes on the wire in a marker's slot (<see cref="SystemsViewProblems.MarkerSlot"/>): never renumber.
    /// </summary>
    public readonly struct SystemsViewProblemKind
    {
        public readonly byte Code;
        public readonly string Name;
        public readonly string Words;

        public SystemsViewProblemKind(byte code, string name, string words)
        {
            Code = code;
            Name = name;
            Words = words;
        }

        /// <summary>The legend line and hover row: "Open pipe end: the pipe stops here, joined to nothing."</summary>
        public string Sentence => $"{Name}: {Words}.";
    }

    /// <summary>
    /// What the problem markers share across domains: their slot code, their look, their legend lines, and the wording
    /// helpers the analyzers build Map Check text with.
    /// </summary>
    public static class SystemsViewProblems
    {
        /// <summary>The marker's colour: a warning red, distinct from every palette colour and status dot.</summary>
        public static readonly Color MarkerColor = new(0.93f, 0.16f, 0.2f, 1f);
        private static readonly Color MarkerEdge = new(0.05f, 0.06f, 0.08f, 0.95f);
        private static readonly Color MarkerGlyph = new(1f, 1f, 1f, 1f);

        /// <summary>
        /// A marker's key slot: the problem kind's code in the high five bits, and a sub-slot (a pipe's line) in the low three,
        /// so two kinds of problem on one tile both show.
        /// </summary>
        public static byte MarkerSlot(byte code, byte sub = 0) => (byte)((code << 3) | (sub & 7));

        /// <summary>The problem kind's code of a marker slot.</summary>
        public static byte CodeOf(byte slot) => (byte)(slot >> 3);

        /// <summary>The kind <paramref name="code"/> names among <paramref name="kinds"/>; false for none.</summary>
        public static bool TryGetKind(IReadOnlyList<SystemsViewProblemKind> kinds, byte code, out SystemsViewProblemKind kind)
        {
            for (int i = 0; kinds != null && i < kinds.Count; i++)
            {
                if (kinds[i].Code == code)
                {
                    kind = kinds[i];
                    return true;
                }
            }

            kind = default;
            return false;
        }

        /// <summary>The legend's problem lines: what a marker is, then one line per kind, each in the words Map Check uses.</summary>
        public static SystemsViewLegendEntry[] Legend(IReadOnlyList<SystemsViewProblemKind> kinds)
        {
            List<SystemsViewLegendEntry> lines = new()
            {
                new("A red diamond marks a problem; point at it to read it. Map Check lists the same ones, with Locate.", MarkerColor),
            };
            foreach (SystemsViewProblemKind kind in kinds)
                lines.Add(new SystemsViewLegendEntry(kind.Sentence));
            return lines.ToArray();
        }

        /// <summary>Puts a marker for every issue into <paramref name="structure"/>; a later issue on the same key wins.</summary>
        public static void AddMarkers(SystemsViewStructure structure, IReadOnlyList<SystemsViewIssue> issues)
        {
            if (structure == null || issues == null)
                return;

            foreach (SystemsViewIssue issue in issues)
            {
                if (issue.Markers == null)
                    continue;

                foreach (SystemsViewKey key in issue.Markers)
                    structure.Set(new SystemsViewMarker { Key = key, IssueId = issue.Id });
            }
        }

        /// <summary>
        /// A marker in a tile's north-east corner (mesh space, at overlay height), so the device badge under it stays
        /// readable: a dark-edged red diamond with a white bar and dot, a warning sign at any zoom.
        /// </summary>
        public static void DrawMarker(SystemsViewMeshBuilder mesh, Vector3 tileCentre)
        {
            Vector3 centre = tileCentre + new Vector3(0.27f, 0f, 0.27f);
            Diamond(mesh, centre, 0.2f, MarkerEdge);
            Diamond(mesh, centre, 0.16f, MarkerColor);
            mesh.AddRect(centre + new Vector3(0f, 0f, 0.025f), 0.022f, 0.055f, MarkerGlyph);
            mesh.AddSquare(centre + new Vector3(0f, 0f, -0.07f), 0.022f, MarkerGlyph);
        }

        private static void Diamond(SystemsViewMeshBuilder mesh, Vector3 centre, float half, Color color) => mesh.AddQuad(
            centre + new Vector3(0f, 0f, -half), centre + new Vector3(-half, 0f, 0f),
            centre + new Vector3(0f, 0f, half), centre + new Vector3(half, 0f, 0f), color);

        /// <summary>"(3, 4)", as every Map Check title names a spot.</summary>
        public static string Cell(Vector2Int tile) => $"({tile.x}, {tile.y})";

        /// <summary>"1 vent", "3 vents".</summary>
        public static string Counted(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

        /// <summary>West to east, then south to north: the order Map Check names devices in, so Locate goes to the first.</summary>
        public static int CompareTiles(Vector2Int a, Vector2Int b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y);

        /// <summary>The world spot a tile's Locate centres on.</summary>
        public static Vector3 TileCentre(Vector2Int tile) => new(tile.x, 0f, tile.y);

        public static readonly IReadOnlyList<SystemsViewIssue> None = Array.Empty<SystemsViewIssue>();

        /// <summary>A room's name in a title; "Unnamed room" when it has none (Map Check's rows and both analyzers).</summary>
        public static string RoomName(string name) => string.IsNullOrWhiteSpace(name) ? "Unnamed room" : name;

        /// <summary>
        /// For items sorted by tile, each one's place among those on the same tile (0 for the first), capped at the 7 a
        /// marker's sub-slot holds: so two devices on one tile with the same kind of problem get a marker each.
        /// </summary>
        public static byte[] IndexOnTile<T>(IReadOnlyList<T> sorted, Func<T, Vector2Int> tileOf)
        {
            byte[] index = new byte[sorted.Count];
            for (int i = 1; i < sorted.Count; i++)
            {
                if (tileOf(sorted[i]) == tileOf(sorted[i - 1]))
                    index[i] = (byte)Math.Min(7, index[i - 1] + 1);
            }

            return index;
        }

        /// <summary>One analyzer finding, as both analyzers build them.</summary>
        public static SystemsViewIssue Issue(string category, string id, string title, string body, Vector3 position,
            SystemsViewKey[] markers, string entryKey = null) => new()
        {
            Id = id,
            Category = category,
            Title = title,
            Body = body,
            Position = position,
            Markers = markers,
            EntryKey = entryKey,
        };
    }
}
