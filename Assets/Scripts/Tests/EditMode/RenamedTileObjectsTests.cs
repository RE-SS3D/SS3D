using NUnit.Framework;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EditorTests
{
    public sealed class RenamedTileObjectsTests
    {
        [TestCase("AtmosPipesL1", "AtmosPipesSupply")]
        [TestCase("AtmosPipesL2", "AtmosPipesMixed")]
        [TestCase("AtmosPipesL3", "AtmosPipesWaste")]
        [TestCase("AtmosPipesL4", "AtmosPipesSurface")]
        [TestCase("atmospipesl1", "AtmosPipesSupply")]
        public void OldPipeNames_ResolveToTheirLine(string savedName, string expected)
        {
            Assert.AreEqual(expected, RenamedTileObjects.Resolve(savedName));
        }

        [TestCase("AtmosPipesSupply")]
        [TestCase("Vent")]
        [TestCase(null)]
        public void OtherNames_PassThrough(string savedName)
        {
            Assert.AreEqual(savedName, RenamedTileObjects.Resolve(savedName));
        }

        [Test]
        public void EveryNewName_IsARealAsset_AndNoOldNameIsLeft()
        {
            HashSet<string> assets = Resources.LoadAll<GenericObjectSo>("").Select(asset => asset.NameString).ToHashSet();

            foreach (KeyValuePair<string, string> rename in RenamedTileObjects.All)
            {
                Assert.IsTrue(assets.Contains(rename.Value), rename.Value);
                Assert.IsFalse(assets.Contains(rename.Key), rename.Key);
            }
        }
    }
}
