using System.IO;
using NUnit.Framework;
using SS3D.Systems.Tile.MapImport.Dmm;
using UnityEngine;

namespace SS3D.Tests.EditMode.MapImport
{
    public class DmmParserTests
    {
        private static string FixturePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath,
                "Scripts/Tests/EditMode/MapImport/Fixtures/tiny_box.dmm"));

        [Test]
        public void Parse_TinyBox_ExpandsPrefabsAndCells()
        {
            DmmMap map = DmmParser.ParseFile(FixturePath);

            Assert.AreEqual(3, map.KeyLength);
            Assert.AreEqual(7, map.Prefabs.Count);
            Assert.AreEqual(18, map.Cells.Count); // 3 cols x 6 rows

            DmmCell door = map.Cells.Find(c => c.Key == "aad");
            Assert.IsNotNull(door);
            Assert.IsTrue(door.Atoms.Exists(a => a.Path.StartsWith("/obj/machinery/door/airlock")));
            Assert.IsTrue(door.Atoms.Exists(a => a.Path.StartsWith("/turf/open/floor")));
            DmmAtom airlock = door.Atoms.Find(a => a.Path.StartsWith("/obj/machinery/door/airlock"));
            Assert.IsTrue(airlock.TryGetInt("dir", out int dir));
            Assert.AreEqual(4, dir);
        }

        [Test]
        public void Parse_ClassicSingleLinePrefab_Works()
        {
            const string text = "\"a\" = (/turf/open/floor/iron,/area/space)\n(1,1,1) = {\"\na\n\"}\n";
            DmmMap map = DmmParser.Parse(text);
            Assert.AreEqual(1, map.Prefabs.Count);
            Assert.AreEqual(1, map.Cells.Count);
            Assert.AreEqual(2, map.Cells[0].Atoms.Count);
        }
    }
}
