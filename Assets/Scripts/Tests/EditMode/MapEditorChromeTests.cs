using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Tile.MapEditor.UI;
using UnityEngine.UIElements;

namespace EditorTests
{
    /// <summary>The map editor's hint bar: key names become chips, the rest reads in lower case.</summary>
    public sealed class MapEditorChromeTests
    {
        [TestCase("Drag to place a line, Shift+drag to fill a rectangle — R to rotate",
            "drag to place a line, |[SHIFT]| |drag to fill a rectangle · |[R]| to rotate")]
        [TestCase("Erase paint — click a square, Shift-drag a rectangle; hold Ctrl to erase",
            "erase paint · click a square, |[SHIFT]| |drag a rectangle; hold |[CTRL]| to erase")]
        [TestCase("Click to place the item", "click to place the item")]
        public void FillHint_DrawsKeyNamesAsChips(string hint, string expected)
        {
            VisualElement bar = new();
            MapEditorChrome.FillHint(bar, hint);

            string drawn = string.Join("|", bar.Children().Cast<Label>()
                .Select(part => part.ClassListContains("map-editor-key") ? $"[{part.text}]" : part.text));
            Assert.AreEqual(expected, drawn);
        }

        [Test]
        public void FillHint_ReplacesThePreviousHint()
        {
            VisualElement bar = new();
            MapEditorChrome.FillHint(bar, "Hold Shift and drag");
            MapEditorChrome.FillHint(bar, "Click to place the item");

            Assert.AreEqual(1, bar.childCount);
        }
    }
}
