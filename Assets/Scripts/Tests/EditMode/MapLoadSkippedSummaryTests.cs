using NUnit.Framework;
using SS3D.Systems.Tile.MapEditor;
using System.Collections.Generic;

namespace SS3D.Tests.EditMode
{
    public sealed class MapLoadSkippedSummaryTests
    {
        [Test]
        public void NothingSkipped_ReturnsNull()
        {
            Assert.IsNull(MapEditorSubSystem.DescribeSkipped(new Dictionary<string, int>()));
            Assert.IsNull(MapEditorSubSystem.DescribeSkipped(null));
        }

        [Test]
        public void ListsMostCommonFirst_AndCollapsesTheRest()
        {
            var skipped = new Dictionary<string, int>
            {
                ["A"] = 1, ["B"] = 5, ["C"] = 2, ["D"] = 3, ["E"] = 4, ["F"] = 1,
            };

            Assert.AreEqual("5 × B, 4 × E, 3 × D, 2 × C and 2 more kinds",
                MapEditorSubSystem.DescribeSkipped(skipped));
        }
    }
}
