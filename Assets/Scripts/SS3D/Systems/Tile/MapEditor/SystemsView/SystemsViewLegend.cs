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
    /// The legend's lines per view, and whether it is open. Each domain registers its own lines with its
    /// <see cref="SystemsViewDomain"/>, next to the strings its badges, markers and Map Check share.
    /// </summary>
    public static class SystemsViewLegend
    {
        public const string OpenPrefsKey = "SS3D.MapEditor.SystemsLegendOpen";

        /// <summary>Three palette colours, for a legend line about network colours.</summary>
        public static readonly Color[] NetworkSwatches =
            { SystemsViewPalette.ColorFor(1), SystemsViewPalette.ColorFor(2), SystemsViewPalette.ColorFor(3) };

        public static string TitleFor(SystemsViewKind view) => view switch
        {
            SystemsViewKind.Atmos => "Atmos view",
            SystemsViewKind.Power => "Power view",
            _ => string.Empty,
        };

        /// <summary>The registered domain's lines (<see cref="SystemsViewDomains"/>); none for a view no domain has registered.</summary>
        public static IReadOnlyList<SystemsViewLegendEntry> EntriesFor(SystemsViewKind view) =>
            SystemsViewDomains.For(view)?.Legend ?? Array.Empty<SystemsViewLegendEntry>();

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
