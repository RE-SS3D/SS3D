using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EditorTests.Atmospherics
{
    /// <summary>
    /// Surface pipe paint jobs are colour only: each must join the grey pipe and every other colour.
    /// </summary>
    public sealed class SurfacePipePaintTests
    {
        private const string FamilyFolder = "Assets/Content/Data/TileMap/Resources/Structures/Pipes/Atmospherics/SurfacePipeVariants";

        [Test]
        public void EveryPaint_ConnectsLikeTheGreyPipe()
        {
            TileObjectSo[] tiles = AssetDatabase.FindAssets("t:TileObjectSo", new[] { FamilyFolder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<TileObjectSo>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToArray();
            TileObjectSo grey = tiles.Single(tile => tile.NameString == "AtmosPipesSurface");

            Assert.That(tiles.Length, Is.EqualTo(8), "grey plus seven paints");
            foreach (TileObjectSo tile in tiles)
            {
                Assert.That(tile.layer, Is.EqualTo(TileLayer.PipeSurface), tile.NameString);
                Assert.That(tile.genericType, Is.EqualTo(grey.genericType), tile.NameString);
                Assert.That(tile.specificType, Is.EqualTo(grey.specificType), tile.NameString);

                GameObject prefab = PrefabOf(tile);
                Assert.That(prefab.GetComponent<SimpleAdjacencyConnector>(), Is.Not.Null, tile.NameString);
                Assert.That(
                    prefab.GetComponent<MeshRenderer>().sharedMaterial,
                    Is.SameAs(PrefabOf(grey).GetComponent<MeshRenderer>().sharedMaterial),
                    tile.NameString + " keeps the shared Palette material");
            }
        }

        private static GameObject PrefabOf(TileObjectSo tile) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(tile.PrefabAsset.Id));
    }
}
