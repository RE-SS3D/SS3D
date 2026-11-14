using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>One legend line: what a marking looks like, in plain words (creative-mode §4 "Plain words").</summary>
    public readonly struct SystemsViewLegendEntry
    {
        public readonly string Text;
        /// <summary>Swatch colours; empty for a line with no swatch.</summary>
        public readonly Color[] Swatches;

        public SystemsViewLegendEntry(string text, params Color[] swatches)
        {
            Text = text;
            Swatches = swatches ?? Array.Empty<Color>();
        }
    }

    /// <summary>
    /// The legend's lines per view, and whether it is open. Later phases add their badge, dot and marker lines here,
    /// next to the strings the markers and Map Check share.
    /// </summary>
    public static class SystemsViewLegend
    {
        public const string OpenPrefsKey = "SS3D.MapEditor.SystemsLegendOpen";

        private static readonly Color[] NetworkSwatches =
            { SystemsViewPalette.ColorFor(1), SystemsViewPalette.ColorFor(2), SystemsViewPalette.ColorFor(3) };

        private static readonly SystemsViewLegendEntry[] Atmos =
        {
            new("Each colour is one pipe network: one connected run on one line. A valve, pump or filter sits between two networks.", NetworkSwatches),
            new("Floors and objects are dimmed so the pipes underneath show through."),
        };

        private static readonly SystemsViewLegendEntry[] Power =
        {
            new("Each colour is one cable circuit, joining generators, SMES units and APCs.", NetworkSwatches),
            new("Floors and objects are dimmed so the cables underneath show through."),
        };

        public static string TitleFor(SystemsViewKind view) => view switch
        {
            SystemsViewKind.Atmos => "Atmos view",
            SystemsViewKind.Power => "Power view",
            _ => string.Empty,
        };

        public static IReadOnlyList<SystemsViewLegendEntry> EntriesFor(SystemsViewKind view) => view switch
        {
            SystemsViewKind.Atmos => Atmos,
            SystemsViewKind.Power => Power,
            _ => Array.Empty<SystemsViewLegendEntry>(),
        };

        /// <summary>Open the first time a view is used, then however the author left it (like <c>MapEditorLastTab</c>).</summary>
        public static bool LoadOpen() => PlayerPrefs.GetInt(OpenPrefsKey, 1) != 0;

        public static void SaveOpen(bool open)
        {
            if (PlayerPrefs.HasKey(OpenPrefsKey) && LoadOpen() == open)
                return;

            PlayerPrefs.SetInt(OpenPrefsKey, open ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
